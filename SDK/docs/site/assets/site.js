(() => {
  const header = document.querySelector('header');
  const mainEl = document.querySelector('body > main');
  const searchInput = document.querySelector('#site-search');
  const searchBox = document.querySelector('#search-results');
  const root = (searchInput && searchInput.dataset.root) || '';
  const items = window.NovaOrynSearchIndex || [];

  /* ---------------- search (existing behaviour, kept as-is) ---------------- */
  if (searchInput && searchBox) {
    searchInput.addEventListener('input', () => {
      const q = searchInput.value.trim().toLowerCase();
      searchBox.innerHTML = '';
      if (q.length < 2) return;
      for (const item of items
        .filter(x => (x.title + ' ' + x.assembly + ' ' + x.summary).toLowerCase().includes(q))
        .slice(0, 8)) {
        const a = document.createElement('a');
        a.href = root + item.url;
        a.textContent = item.title + ' — ' + item.assembly;
        searchBox.appendChild(a);
      }
    });
    document.addEventListener('click', (e) => {
      if (e.target !== searchInput && !searchBox.contains(e.target)) searchBox.innerHTML = '';
    });
  }

  if (!header || !mainEl) return;

  /* ---------------- figure out the current page's site-relative path ---------------- */
  const depth = (root.match(/\.\.\//g) || []).length;
  const segs = window.location.pathname.split('/').filter(Boolean);
  const relPath = segs.slice(segs.length - (depth + 1)).join('/');

  /* ---------------- build the two-pane shell ---------------- */
  const shell = document.createElement('div');
  shell.className = 'shell';

  const aside = document.createElement('aside');
  aside.className = 'sidebar';
  aside.id = 'site-sidebar';
  const inner = document.createElement('div');
  inner.className = 'sidebar-inner';
  aside.appendChild(inner);

  const contentDiv = document.createElement('div');
  contentDiv.className = 'content';

  header.insertAdjacentElement('afterend', shell);
  shell.appendChild(aside);
  shell.appendChild(contentDiv);
  contentDiv.appendChild(mainEl);
  const footerEl = document.querySelector('body > footer');
  if (footerEl) contentDiv.appendChild(footerEl);

  /* mobile contents toggle */
  const toggle = document.createElement('button');
  toggle.type = 'button';
  toggle.className = 'sidebar-toggle';
  toggle.setAttribute('aria-label', 'Toggle contents');
  toggle.textContent = '☰';
  toggle.addEventListener('click', () => document.body.classList.toggle('sidebar-open'));
  header.insertBefore(toggle, header.firstChild);
  document.addEventListener('click', (e) => {
    if (document.body.classList.contains('sidebar-open') &&
        !aside.contains(e.target) && e.target !== toggle) {
      document.body.classList.remove('sidebar-open');
    }
  });

  /* ---------------- helpers ---------------- */
  function makeLink(text, url, extraClass) {
    const a = document.createElement('a');
    a.href = root + url;
    a.textContent = text;
    if (extraClass) a.className = extraClass;
    if (url === relPath) a.classList.add('active');
    return a;
  }
  const shortAsm = (name) => name.startsWith('NovaOryn.') ? name.slice('NovaOryn.'.length) : name;
  const shortItem = (title, assembly) =>
    title.startsWith(assembly + '.') ? title.slice(assembly.length + 1) : title;

  /* ---------------- build nav once nav-data.js is available ---------------- */
  function buildNav(nav) {
    /* Guides */
    const guidesGroup = document.createElement('div');
    guidesGroup.className = 'nav-group';
    const gh = document.createElement('h3');
    gh.textContent = 'Guides';
    guidesGroup.appendChild(gh);
    const gul = document.createElement('ul');
    for (const g of nav.guides) {
      const li = document.createElement('li');
      li.appendChild(makeLink(g.title, g.url, 'nav-link'));
      gul.appendChild(li);
    }
    guidesGroup.appendChild(gul);
    inner.appendChild(guidesGroup);

    /* group assemblies by their second namespace segment */
    const byCat = new Map();
    for (const a of nav.assemblies) {
      const cat = a.name.startsWith('NovaOryn.') ? a.name.split('.')[1] : 'Samples';
      if (!byCat.has(cat)) byCat.set(cat, []);
      byCat.get(cat).push(a);
    }
    const multi = [];
    const single = [];
    for (const [cat, list] of byCat) {
      if (list.length > 1) multi.push([cat, list]);
      else single.push(list[0]);
    }
    multi.sort((a, b) => a[0].localeCompare(b[0]));
    single.sort((a, b) => a.name.localeCompare(b.name));
    if (single.length) multi.push(['Other modules', single]);

    /* group public items (from the search index) by assembly */
    const itemsByAssembly = new Map();
    for (const it of items) {
      if (!itemsByAssembly.has(it.assembly)) itemsByAssembly.set(it.assembly, []);
      itemsByAssembly.get(it.assembly).push(it);
    }

    const asmGroup = document.createElement('div');
    asmGroup.className = 'nav-group';
    const ah = document.createElement('h3');
    ah.textContent = 'SDK Assemblies';
    asmGroup.appendChild(ah);
    const aul = document.createElement('ul');

    let activeDetails = [];

    for (const [cat, list] of multi) {
      const li = document.createElement('li');
      const det = document.createElement('details');
      det.className = 'nav-node nav-category';
      const sum = document.createElement('summary');
      const label = document.createElement('span');
      label.textContent = cat;
      sum.appendChild(label);
      sum.appendChild(document.createTextNode(' '));
      const cnt = document.createElement('span');
      cnt.className = 'count';
      cnt.textContent = list.length;
      sum.appendChild(cnt);
      det.appendChild(sum);

      const ul2 = document.createElement('ul');
      for (const asm of list) {
        const li2 = document.createElement('li');
        const asmItems = (itemsByAssembly.get(asm.name) || []).slice()
          .sort((a, b) => a.title.localeCompare(b.title));

        let asmIsActive = asm.url === relPath;

        if (asmItems.length) {
          const det2 = document.createElement('details');
          det2.className = 'nav-node nav-assembly';
          const sum2 = document.createElement('summary');
          const asmLink = makeLink(shortAsm(asm.name), asm.url);
          asmLink.title = asm.name;
          sum2.appendChild(asmLink);
          sum2.appendChild(document.createTextNode(' '));
          const cnt2 = document.createElement('span');
          cnt2.className = 'count';
          cnt2.textContent = asm.count;
          sum2.appendChild(cnt2);
          det2.appendChild(sum2);

          const iul = document.createElement('ul');
          iul.className = 'nav-items';
          for (const it of asmItems) {
            const ili = document.createElement('li');
            const a3 = makeLink(shortItem(it.title, asm.name), it.url);
            a3.appendChild(document.createTextNode(' '));
            const kindSpan = document.createElement('span');
            kindSpan.className = 'item-kind';
            kindSpan.textContent = it.kind;
            a3.appendChild(kindSpan);
            ili.appendChild(a3);
            iul.appendChild(ili);
            if (it.url === relPath) { det2.open = true; asmIsActive = true; activeDetails.push(a3); }
          }
          det2.appendChild(iul);
          li2.appendChild(det2);
          if (asm.url === relPath) det2.open = true;
        } else {
          li2.appendChild(makeLink(shortAsm(asm.name), asm.url, 'nav-link'));
        }
        ul2.appendChild(li2);
        if (asmIsActive) { det.open = true; }
      }
      det.appendChild(ul2);
      li.appendChild(det);
      aul.appendChild(li);
    }
    asmGroup.appendChild(aul);
    inner.appendChild(asmGroup);

    /* bring the active entry into view within the sidebar */
    (window.requestAnimationFrame || window.setTimeout)(() => {
      const active = inner.querySelector('a.active');
      if (active && active.scrollIntoView) active.scrollIntoView({ block: 'center' });
    });
  }

  if (window.NovaOrynNav) {
    buildNav(window.NovaOrynNav);
  } else {
    const s = document.createElement('script');
    s.src = root + 'assets/nav-data.js';
    s.onload = () => buildNav(window.NovaOrynNav);
    document.head.appendChild(s);
  }
})();
