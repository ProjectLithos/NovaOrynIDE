using System;

namespace NovaOryn.Kernel.Console;

internal unsafe struct FramebufferConsole
{
    internal const UInt32 SmallFontSize = 8U;
    internal const UInt32 MediumFontSize = 16U;
    internal const UInt32 LargeFontSize = 24U;
    internal const UInt32 ScrollbackCapacity = 262144U;
    internal const UInt32 ScrollbarWidth = 10U;
    internal const UInt32 CaretBlinkTicks = 500U;

    private UInt64 _address;
    private UInt64 _size;
    private UInt32 _width;
    private UInt32 _height;
    private UInt32 _pitch;
    private UInt32 _pixelFormat;
    private UInt32 _redMask;
    private UInt32 _greenMask;
    private UInt32 _blueMask;
    private UInt32 _cursorX;
    private UInt32 _cursorY;
    private UInt32 _fontSize;
    private UInt32 _glyphWidth;
    private UInt32 _characterAdvance;
    private UInt32 _lineHeight;
    private UInt32 _margin;
    private UInt32 _foreground;
    private UInt32 _background;
    private UInt32 _historyStart;
    private UInt32 _historyLength;
    private UInt32 _scrollLinesFromBottom;
    private UInt64 _backBufferA;
    private UInt64 _backBufferB;
    private UInt64 _drawBuffer;
    private UInt64 _frameByteCount;
    private UInt32 _availableBufferCount;
    private UInt32 _bufferCount;
    private Boolean _automaticBuffering;
    private Boolean _dirty;
    private Boolean _caretEnabled;
    private Boolean _caretVisible;
    private UInt32 _caretTicks;
    private UInt32 _dirtyLeft;
    private UInt32 _dirtyTop;
    private UInt32 _dirtyRight;
    private UInt32 _dirtyBottom;
    private fixed Byte _history[(Int32)ScrollbackCapacity];

    internal UInt32 FontSize
    {
        get { return _fontSize; }
    }

    internal UInt32 ScrollLinesFromBottom
    {
        get { return _scrollLinesFromBottom; }
    }

    internal UInt64 FrameByteCount
    {
        get { return _frameByteCount; }
    }

    internal UInt32 BufferCount
    {
        get { return _bufferCount; }
    }

    internal UInt32 AvailableBufferCount
    {
        get { return _availableBufferCount; }
    }

    internal Boolean AutomaticBuffering
    {
        get { return _automaticBuffering; }
    }

    internal Boolean Initialize(BootContext boot, UInt32 fontSize)
    {
        NativeBootContext* context = boot.GetNativeContext();
        if (context == null || context->Signature != 0x4E59524F41564F4EUL) return false;
        if (context->FramebufferAddress == 0 || (context->FramebufferAddress & 3UL) != 0) return false;
        if (context->FramebufferSize == 0 || context->Width == 0 || context->Height == 0) return false;
        if (context->PixelsPerScanLine < context->Width) return false;
        if (context->PixelFormat > 2U) return false;

        UInt64 bytesPerScanLine = (UInt64)context->PixelsPerScanLine * 4UL;
        if (bytesPerScanLine == 0 || (UInt64)context->Height > context->FramebufferSize / bytesPerScanLine) return false;
        if (context->PixelFormat == 2U)
        {
            if (context->RedMask == 0 || context->GreenMask == 0 || context->BlueMask == 0) return false;
            if ((context->RedMask & context->GreenMask) != 0) return false;
            if ((context->RedMask & context->BlueMask) != 0) return false;
            if ((context->GreenMask & context->BlueMask) != 0) return false;
            if (context->ReservedMask != 0U)
            {
                if ((context->ReservedMask & context->RedMask) != 0) return false;
                if ((context->ReservedMask & context->GreenMask) != 0) return false;
                if ((context->ReservedMask & context->BlueMask) != 0) return false;
            }
            if (!IsContiguousMask(context->RedMask)) return false;
            if (!IsContiguousMask(context->GreenMask)) return false;
            if (!IsContiguousMask(context->BlueMask)) return false;
        }

        _address = context->FramebufferAddress;
        _size = context->FramebufferSize;
        _width = context->Width;
        _height = context->Height;
        _pitch = context->PixelsPerScanLine;
        _pixelFormat = context->PixelFormat;
        _redMask = context->RedMask;
        _greenMask = context->GreenMask;
        _blueMask = context->BlueMask;
        _foreground = PackColor(232, 240, 248);
        _background = PackColor(9, 16, 24);
        _historyStart = 0U;
        _historyLength = 0U;
        _scrollLinesFromBottom = 0U;
        _frameByteCount = bytesPerScanLine * (UInt64)context->Height;
        _backBufferA = 0UL;
        _backBufferB = 0UL;
        _drawBuffer = _address;
        _availableBufferCount = 1U;
        _bufferCount = 1U;
        _automaticBuffering = true;
        _caretEnabled = false;
        _caretVisible = false;
        _caretTicks = 0U;
        ResetDirty();
        return ConfigureFont(fontSize);
    }

    internal Boolean Clear()
    {
        _historyStart = 0U;
        _historyLength = 0U;
        _scrollLinesFromBottom = 0U;
        _caretVisible = false;
        _caretTicks = 0U;
        if (!ClearPixels()) return false;
        _cursorX = _margin;
        _cursorY = _margin;
        return Present();
    }

    internal Boolean Write(Byte value)
    {
        if (value == (Byte)'\r') return true;
        if (!HideCaret()) return false;
        UInt32 linesBefore = _scrollLinesFromBottom == 0U ? 0U : CountVisualLines();
        AppendHistory(value);
        if (_scrollLinesFromBottom != 0U)
        {
            UInt32 linesAfter = CountVisualLines();
            if (linesAfter > linesBefore) _scrollLinesFromBottom += linesAfter - linesBefore;
            return true;
        }

        UInt32 previousX = _cursorX;
        UInt32 previousY = _cursorY;
        if (!RenderLive(value)) return false;

        // Text is rendered glyph-by-glyph into the active render buffer, but presentation is
        // deliberately line/region based. Present once when a newline/wrap completes a line;
        // otherwise keep accumulating the dirty rectangle for the current line.
        Boolean completedLine = value == (Byte)'\n' || _cursorY != previousY || _cursorX < previousX;
        return !completedLine || Present();
    }

    internal Boolean Backspace()
    {
        if (!HideCaret()) return false;
        if (_historyLength == 0U) return Flush();
        Byte last = GetHistoryByte(_historyLength - 1U);
        if (last == (Byte)'\n') return true;
        _historyLength--;
        if (!RedrawHistory()) return false;
        return Flush();
    }

    internal Boolean Flush()
    {
        if (!DrawScrollbar()) return false;
        if (_caretEnabled && _scrollLinesFromBottom == 0U && !_caretVisible)
        {
            if (!DrawCaret(_foreground)) return false;
            _caretVisible = true;
        }
        return Present();
    }

    internal Boolean SetCaretEnabled(Boolean enabled)
    {
        if (!HideCaret()) return false;
        _caretEnabled = enabled;
        _caretTicks = 0U;
        return Flush();
    }

    internal Boolean TickCaret()
    {
        if (!_caretEnabled || _scrollLinesFromBottom != 0U) return true;
        _caretTicks++;
        if (_caretTicks < CaretBlinkTicks) return true;
        _caretTicks = 0U;
        if (_caretVisible)
        {
            if (!DrawCaret(_background)) return false;
            _caretVisible = false;
        }
        else
        {
            if (!DrawCaret(_foreground)) return false;
            _caretVisible = true;
        }
        if (!DrawScrollbar()) return false;
        return Present();
    }

    internal Boolean ConfigureBuffers(UInt64 backBufferA, UInt64 backBufferB, UInt64 bufferByteCount)
    {
        if (_address == 0UL || _frameByteCount == 0UL) return false;
        if (backBufferA == 0UL || backBufferB == 0UL || backBufferA == backBufferB) return false;
        if ((backBufferA & 3UL) != 0UL || (backBufferB & 3UL) != 0UL) return false;
        if (backBufferA == _address || backBufferB == _address) return false;
        if (bufferByteCount < _frameByteCount) return false;
        _backBufferA = backBufferA;
        _backBufferB = backBufferB;
        _availableBufferCount = 3U;
        if (!CopyFrame(_address, _backBufferA)) return false;
        if (!CopyFrame(_address, _backBufferB)) return false;
        return SetBufferCount(0U);
    }

    internal Boolean SetBufferCount(UInt32 bufferCount)
    {
        // 0 = automatic. The framebuffer text console benefits from one software render buffer
        // without the extra synchronization copy required by triple buffering.
        Boolean automatic = bufferCount == 0U;
        if (automatic) bufferCount = _availableBufferCount >= 2U ? 2U : 1U;
        if (bufferCount < 1U || bufferCount > 3U) return false;
        if (bufferCount > _availableBufferCount) return false;
        if (_bufferCount > 1U && !Present()) return false;
        if (bufferCount == 1U)
        {
            _bufferCount = 1U;
            _drawBuffer = _address;
            _automaticBuffering = automatic;
            return true;
        }
        if (_backBufferA == 0UL) return false;
        if (!CopyFrame(_address, _backBufferA)) return false;
        if (bufferCount == 3U)
        {
            if (_backBufferB == 0UL) return false;
            if (!CopyFrame(_address, _backBufferB)) return false;
        }
        _bufferCount = bufferCount;
        _drawBuffer = _backBufferA;
        _automaticBuffering = automatic;
        return true;
    }

    internal Boolean ScrollUp()
    {
        if (!HideCaret()) return false;
        UInt32 totalLines = CountVisualLines();
        UInt32 visibleLines = GetVisibleLineCount();
        UInt32 maximumOffset = totalLines > visibleLines ? totalLines - visibleLines : 0U;
        if (_scrollLinesFromBottom < maximumOffset) _scrollLinesFromBottom++;
        if (!RedrawHistory()) return false;
        return Flush();
    }

    internal Boolean ScrollDown()
    {
        if (!HideCaret()) return false;
        if (_scrollLinesFromBottom != 0U) _scrollLinesFromBottom--;
        if (!RedrawHistory()) return false;
        return Flush();
    }

    internal Boolean SetFontPreset(UInt32 preset)
    {
        UInt32 size;
        if (preset == 1U) size = SmallFontSize;
        else if (preset == 2U) size = MediumFontSize;
        else if (preset == 3U) size = LargeFontSize;
        else return false;
        if (!ConfigureFont(size)) return false;
        UInt32 totalLines = CountVisualLines();
        UInt32 visibleLines = GetVisibleLineCount();
        UInt32 maximumOffset = totalLines > visibleLines ? totalLines - visibleLines : 0U;
        if (_scrollLinesFromBottom > maximumOffset) _scrollLinesFromBottom = maximumOffset;
        if (!RedrawHistory()) return false;
        return Present();
    }

    internal Boolean ReloadFontFace()
    {
        if (!ConfigureFont(_fontSize)) return false;
        UInt32 totalLines = CountVisualLines();
        UInt32 visibleLines = GetVisibleLineCount();
        UInt32 maximumOffset = totalLines > visibleLines ? totalLines - visibleLines : 0U;
        if (_scrollLinesFromBottom > maximumOffset) _scrollLinesFromBottom = maximumOffset;
        if (!RedrawHistory()) return false;
        return Present();
    }

    private Boolean ConfigureFont(UInt32 fontSize)
    {
        if (fontSize < BitmapFont.MinimumFontSize || fontSize > BitmapFont.MaximumFontSize) return false;
        UInt32 glyphWidth = ConsoleFont.GetRenderedGlyphWidth(fontSize);
        UInt32 characterAdvance = ConsoleFont.GetRenderedCharacterAdvance(fontSize);
        UInt32 lineHeight = ConsoleFont.GetRenderedLineHeight(fontSize);
        UInt32 margin = fontSize / 2U;
        if (margin == 0U) margin = 1U;
        if (glyphWidth == 0U || characterAdvance < glyphWidth || lineHeight < fontSize) return false;
        if (margin >= _width || margin >= _height) return false;
        if (glyphWidth > _width - margin || fontSize > _height - margin) return false;
        _fontSize = fontSize;
        _glyphWidth = glyphWidth;
        _characterAdvance = characterAdvance;
        _lineHeight = lineHeight;
        _margin = margin;
        _cursorX = margin;
        _cursorY = margin;
        return true;
    }

    private Boolean RenderLive(Byte value)
    {
        if (value == (Byte)'\n') return MoveToNextLine();
        UInt32 right = GetTextRight();
        if ((_cursorX >= right || _glyphWidth > right - _cursorX) && !MoveToNextLine()) return false;
        if (_cursorY > _height - _fontSize) return false;
        if (!DrawGlyph(value, _cursorX, _cursorY)) return false;
        _cursorX += _characterAdvance;
        return true;
    }

    private void AppendHistory(Byte value)
    {
        fixed (Byte* history = _history)
        {
            if (_historyLength < ScrollbackCapacity)
            {
                UInt32 index = (_historyStart + _historyLength) % ScrollbackCapacity;
                history[index] = value;
                _historyLength++;
                return;
            }
            history[_historyStart] = value;
            _historyStart++;
            if (_historyStart == ScrollbackCapacity) _historyStart = 0U;
        }
    }

    private Byte GetHistoryByte(UInt32 logicalIndex)
    {
        fixed (Byte* history = _history)
        {
            UInt32 index = (_historyStart + logicalIndex) % ScrollbackCapacity;
            return history[index];
        }
    }

    private UInt32 GetColumnCount()
    {
        UInt32 right = GetTextRight();
        if (_characterAdvance == 0U || _glyphWidth == 0U || _margin >= right || _glyphWidth > right - _margin) return 1U;
        return ((right - _glyphWidth - _margin) / _characterAdvance) + 1U;
    }

    private UInt32 GetVisibleLineCount()
    {
        if (_lineHeight == 0U || _margin > _height - _fontSize) return 1U;
        return ((_height - _fontSize - _margin) / _lineHeight) + 1U;
    }

    private UInt32 CountVisualLines()
    {
        UInt32 columns = GetColumnCount();
        UInt32 lineCount = 1U;
        UInt32 column = 0U;
        UInt32 index = 0U;
        while (index < _historyLength)
        {
            Byte value = GetHistoryByte(index);
            if (value == (Byte)'\n')
            {
                lineCount++;
                column = 0U;
            }
            else
            {
                if (column >= columns)
                {
                    lineCount++;
                    column = 0U;
                }
                column++;
            }
            index++;
        }
        return lineCount;
    }

    private UInt32 FindVisualLineStart(UInt32 targetLine)
    {
        if (targetLine == 0U) return 0U;
        UInt32 columns = GetColumnCount();
        UInt32 line = 0U;
        UInt32 column = 0U;
        UInt32 index = 0U;
        while (index < _historyLength)
        {
            Byte value = GetHistoryByte(index);
            if (value == (Byte)'\n')
            {
                line++;
                column = 0U;
                index++;
                if (line == targetLine) return index;
                continue;
            }
            if (column >= columns)
            {
                line++;
                column = 0U;
                if (line == targetLine) return index;
            }
            column++;
            index++;
        }
        return _historyLength;
    }

    private Boolean RedrawHistory()
    {
        if (!ClearPixels()) return false;
        UInt32 totalLines = CountVisualLines();
        UInt32 visibleLines = GetVisibleLineCount();
        UInt32 maximumOffset = totalLines > visibleLines ? totalLines - visibleLines : 0U;
        if (_scrollLinesFromBottom > maximumOffset) _scrollLinesFromBottom = maximumOffset;
        UInt32 firstLine = totalLines > visibleLines + _scrollLinesFromBottom
            ? totalLines - visibleLines - _scrollLinesFromBottom
            : 0U;
        UInt32 index = FindVisualLineStart(firstLine);
        UInt32 x = _margin;
        UInt32 y = _margin;
        while (index < _historyLength)
        {
            Byte value = GetHistoryByte(index);
            if (value == (Byte)'\n')
            {
                x = _margin;
                if (y > _height - _fontSize || _lineHeight > (_height - _fontSize) - y) break;
                y += _lineHeight;
                index++;
                continue;
            }
            UInt32 right = GetTextRight();
            if (x >= right || _glyphWidth > right - x)
            {
                x = _margin;
                if (y > _height - _fontSize || _lineHeight > (_height - _fontSize) - y) break;
                y += _lineHeight;
            }
            if (y > _height - _fontSize) break;
            if (!DrawGlyph(value, x, y)) return false;
            x += _characterAdvance;
            index++;
        }
        if (_scrollLinesFromBottom == 0U)
        {
            _cursorX = x;
            _cursorY = y;
        }
        return true;
    }

    private Boolean ClearPixels()
    {
        UInt64 pixelCount = (UInt64)_pitch * (UInt64)_height;
        if (pixelCount > _size / 4UL) return false;
        UInt32* pixel = (UInt32*)GetRenderAddress();
        while (pixelCount != 0UL)
        {
            *pixel = _background;
            pixel++;
            pixelCount--;
        }
        MarkDirtyRectangle(0U, 0U, _width, _height);
        return true;
    }

    private Boolean MoveToNextLine()
    {
        _cursorX = _margin;
        if (_cursorY <= _height - _fontSize && _lineHeight <= (_height - _fontSize) - _cursorY)
        {
            _cursorY += _lineHeight;
            return true;
        }
        return ScrollUpOneLinePixels();
    }

    private Boolean ScrollUpOneLinePixels()
    {
        if (_lineHeight == 0U || _margin >= _height || _lineHeight >= _height - _margin) return false;
        UInt32 sourceY = _margin + _lineHeight;
        UInt32 destinationY = _margin;
        UInt32 rowsToMove = _height - sourceY;
        UInt64 framebufferPixels = _size / 4UL;
        UInt32* pixels = (UInt32*)GetRenderAddress();

        UInt32 row = 0U;
        while (row < rowsToMove)
        {
            UInt64 sourceIndex = ((UInt64)(sourceY + row) * (UInt64)_pitch);
            UInt64 destinationIndex = ((UInt64)(destinationY + row) * (UInt64)_pitch);
            if (sourceIndex + _pitch > framebufferPixels || destinationIndex + _pitch > framebufferPixels) return false;
            UInt32 column = 0U;
            while (column < _pitch)
            {
                pixels[destinationIndex + column] = pixels[sourceIndex + column];
                column++;
            }
            row++;
        }

        UInt32 clearStartY = _height - _lineHeight;
        row = clearStartY;
        while (row < _height)
        {
            UInt64 destinationIndex = ((UInt64)row * (UInt64)_pitch);
            if (destinationIndex + _pitch > framebufferPixels) return false;
            UInt32 column = 0U;
            while (column < _pitch)
            {
                pixels[destinationIndex + column] = _background;
                column++;
            }
            row++;
        }
        MarkDirtyRectangle(0U, _margin, _width, _height - _margin);
        return _cursorY <= _height - _fontSize;
    }

    private UInt32 GetTextRight()
    {
        UInt32 reserve = ScrollbarWidth + 2U;
        return _width > reserve ? _width - reserve : _width;
    }

    private Boolean HideCaret()
    {
        if (!_caretVisible) return true;
        if (!DrawCaret(_background)) return false;
        _caretVisible = false;
        _caretTicks = 0U;
        return true;
    }

    private Boolean DrawCaret(UInt32 color)
    {
        if (_cursorX >= GetTextRight() || _cursorY >= _height) return true;
        UInt32 caretWidth = _glyphWidth >= 12U ? 2U : 1U;
        UInt32 caretHeight = _fontSize;
        if (_cursorX + caretWidth > GetTextRight()) caretWidth = 1U;
        return FillRectangle(_cursorX, _cursorY, caretWidth, caretHeight, color);
    }

    private Boolean DrawScrollbar()
    {
        if (_width <= ScrollbarWidth || _height == 0U) return true;
        UInt32 left = _width - ScrollbarWidth;
        UInt32 track = PackColor(27, 38, 49);
        UInt32 thumb = PackColor(112, 132, 150);
        if (!FillRectangle(left, 0U, ScrollbarWidth, _height, track)) return false;
        UInt32 totalLines = CountVisualLines();
        UInt32 visibleLines = GetVisibleLineCount();
        if (totalLines <= visibleLines) return FillRectangle(left + 2U, 2U, ScrollbarWidth - 4U, _height > 4U ? _height - 4U : 1U, thumb);
        UInt32 usable = _height > 4U ? _height - 4U : 1U;
        UInt64 scaled = ((UInt64)visibleLines * usable) / totalLines;
        UInt32 thumbHeight = scaled < 16UL ? 16U : (scaled > usable ? usable : (UInt32)scaled);
        UInt32 maximumOffset = totalLines - visibleLines;
        UInt32 offset = _scrollLinesFromBottom > maximumOffset ? maximumOffset : _scrollLinesFromBottom;
        UInt32 travel = usable > thumbHeight ? usable - thumbHeight : 0U;
        UInt32 fromTop = maximumOffset == 0U ? 0U : (UInt32)(((UInt64)(maximumOffset - offset) * travel) / maximumOffset);
        return FillRectangle(left + 2U, 2U + fromTop, ScrollbarWidth - 4U, thumbHeight, thumb);
    }

    private Boolean FillRectangle(UInt32 left, UInt32 top, UInt32 width, UInt32 height, UInt32 color)
    {
        if (width == 0U || height == 0U || left >= _width || top >= _height) return true;
        UInt32 right = left + width > _width ? _width : left + width;
        UInt32 bottom = top + height > _height ? _height : top + height;
        UInt32* pixels = (UInt32*)GetRenderAddress();
        UInt32 y = top;
        while (y < bottom)
        {
            UInt64 row = (UInt64)y * _pitch;
            UInt32 x = left;
            while (x < right)
            {
                UInt64 index = row + x;
                if (index >= _size / 4UL) return false;
                pixels[index] = color;
                x++;
            }
            y++;
        }
        MarkDirtyRectangle(left, top, right - left, bottom - top);
        return true;
    }

    private Boolean DrawGlyph(Byte value, UInt32 originX, UInt32 originY)
    {
        UInt32 renderedRow = 0U;
        while (renderedRow < _fontSize)
        {
            UInt32 sourceRow = ConsoleFont.GetSourceRow(renderedRow, _fontSize);
            UInt32 bits = ConsoleFont.GetGlyphRow(value, sourceRow);
            UInt32 renderedColumn = 0U;
            while (renderedColumn < _glyphWidth)
            {
                UInt32 sourceColumn = ConsoleFont.GetSourceColumn(renderedColumn, _glyphWidth);
                UInt32 sourceWidth = ConsoleFont.GetSourceWidth();
                UInt32 mask = 1U << (Int32)((sourceWidth - 1U) - sourceColumn);
                if ((bits & mask) != 0U)
                {
                    if (!DrawPixel(originX + renderedColumn, originY + renderedRow)) return false;
                }
                renderedColumn++;
            }
            renderedRow++;
        }
        MarkDirtyRectangle(originX, originY, _glyphWidth, _fontSize);
        return true;
    }

    private Boolean DrawPixel(UInt32 pixelX, UInt32 pixelY)
    {
        if (pixelX >= _width || pixelY >= _height) return false;
        UInt64 index = ((UInt64)pixelY * (UInt64)_pitch) + pixelX;
        if (index >= _size / 4UL) return false;
        *((UInt32*)GetRenderAddress() + index) = _foreground;
        return true;
    }

    private UInt64 GetRenderAddress()
    {
        return _bufferCount == 1U ? _address : _drawBuffer;
    }

    private Boolean Present()
    {
        if (!_dirty) return true;
        if (_bufferCount == 1U)
        {
            ResetDirty();
            return true;
        }
        if (_drawBuffer == 0UL || !CopyRegion(_drawBuffer, _address, _dirtyLeft, _dirtyTop, _dirtyRight, _dirtyBottom)) return false;
        if (_bufferCount == 2U)
        {
            ResetDirty();
            return true;
        }
        UInt64 next = _drawBuffer == _backBufferA ? _backBufferB : _backBufferA;
        if (next == 0UL || !CopyRegion(_drawBuffer, next, _dirtyLeft, _dirtyTop, _dirtyRight, _dirtyBottom)) return false;
        _drawBuffer = next;
        ResetDirty();
        return true;
    }

    private Boolean CopyRegion(UInt64 sourceAddress, UInt64 destinationAddress, UInt32 left, UInt32 top, UInt32 right, UInt32 bottom)
    {
        if (sourceAddress == 0UL || destinationAddress == 0UL) return false;
        if ((sourceAddress & 3UL) != 0UL || (destinationAddress & 3UL) != 0UL) return false;
        if (left >= right || top >= bottom || right > _width || bottom > _height) return false;
        UInt32* source = (UInt32*)sourceAddress;
        UInt32* destination = (UInt32*)destinationAddress;
        UInt32 row = top;
        while (row < bottom)
        {
            UInt64 rowStart = (UInt64)row * (UInt64)_pitch;
            UInt64 start = rowStart + left;
            UInt64 end = rowStart + right;
            if (end > _size / 4UL) return false;
            UInt64 index = start;
            while (index < end)
            {
                destination[index] = source[index];
                index++;
            }
            row++;
        }
        return true;
    }

    private void MarkDirtyRectangle(UInt32 left, UInt32 top, UInt32 width, UInt32 height)
    {
        if (width == 0U || height == 0U || left >= _width || top >= _height) return;
        UInt64 right64 = (UInt64)left + width;
        UInt64 bottom64 = (UInt64)top + height;
        UInt32 right = right64 > _width ? _width : (UInt32)right64;
        UInt32 bottom = bottom64 > _height ? _height : (UInt32)bottom64;
        if (right <= left || bottom <= top) return;
        if (!_dirty)
        {
            _dirty = true;
            _dirtyLeft = left;
            _dirtyTop = top;
            _dirtyRight = right;
            _dirtyBottom = bottom;
            return;
        }
        if (left < _dirtyLeft) _dirtyLeft = left;
        if (top < _dirtyTop) _dirtyTop = top;
        if (right > _dirtyRight) _dirtyRight = right;
        if (bottom > _dirtyBottom) _dirtyBottom = bottom;
    }

    private void ResetDirty()
    {
        _dirty = false;
        _dirtyLeft = 0U;
        _dirtyTop = 0U;
        _dirtyRight = 0U;
        _dirtyBottom = 0U;
    }

    private Boolean CopyFrame(UInt64 sourceAddress, UInt64 destinationAddress)
    {
        if (sourceAddress == 0UL || destinationAddress == 0UL) return false;
        if ((sourceAddress & 3UL) != 0UL || (destinationAddress & 3UL) != 0UL) return false;
        UInt64 pixelCount = _frameByteCount / 4UL;
        if (pixelCount == 0UL || _frameByteCount > _size) return false;
        UInt32* source = (UInt32*)sourceAddress;
        UInt32* destination = (UInt32*)destinationAddress;
        UInt64 index = 0UL;
        while (index < pixelCount)
        {
            destination[index] = source[index];
            index++;
        }
        return true;
    }

    private UInt32 PackColor(Byte red, Byte green, Byte blue)
    {
        if (_pixelFormat == 0U) return (UInt32)red | ((UInt32)green << 8) | ((UInt32)blue << 16);
        if (_pixelFormat == 1U) return (UInt32)blue | ((UInt32)green << 8) | ((UInt32)red << 16);
        return EncodeMask(red, _redMask) | EncodeMask(green, _greenMask) | EncodeMask(blue, _blueMask);
    }

    private static Boolean IsContiguousMask(UInt32 mask)
    {
        while ((mask & 1U) == 0U) mask >>= 1;
        while ((mask & 1U) != 0U) mask >>= 1;
        return mask == 0U;
    }

    private static UInt32 EncodeMask(Byte component, UInt32 mask)
    {
        UInt32 shift = 0U;
        while (((mask >> (Int32)shift) & 1U) == 0U && shift < 31U) shift++;
        UInt32 shiftedMask = mask >> (Int32)shift;
        UInt32 bits = 0U;
        while ((shiftedMask & 1U) != 0U)
        {
            bits++;
            shiftedMask >>= 1;
        }
        UInt64 maximum = bits == 32U ? 0xFFFFFFFFUL : ((1UL << (Int32)bits) - 1UL);
        UInt32 encoded = (UInt32)(((UInt64)component * maximum) / 255UL);
        return (encoded << (Int32)shift) & mask;
    }
}
