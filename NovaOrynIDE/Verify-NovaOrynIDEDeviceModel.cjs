const fs = require('fs');
const path = require('path');
const root = __dirname;
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const requireText = (file, patterns) => {
  const text = read(file);
  for (const pattern of patterns) if (!text.includes(pattern)) throw new Error(`${file}: missing ${pattern}`);
};
requireText('SDK/src/NovaOryn.Kernel.Drivers/KernelDriverContracts.cs', [
  'Platform=1, Pci=2, Usb=3, Acpi=4, Virtual=5, Logical=6',
  'Virtio=Virtual', 'Synthetic=Logical', 'KernelDeviceTreeSnapshot'
]);
requireText('SDK/src/NovaOryn.Kernel.Drivers/KernelDrivers.cs', [
  'DiscoverDevice(KernelDeviceIdentifier identifier,KernelDeviceHandle parent',
  'TryGetDeviceNodeByIndex', 'TryGetRootDevice', 'GetDeviceTreeSnapshot', '_deviceTreeGeneration'
]);
requireText('SDK/src/NovaOryn.Bus.Usb/KernelUsbBus.cs', [
  'KernelDrivers.DiscoverDevice(id,parentHandle',
  'KernelDrivers.DiscoverDevice(iid,new KernelDeviceHandle(owner->Generic)'
]);
requireText('SDK/src/NovaOryn.Kernel.Ahci/KernelAhci.cs', ['KernelDeviceBus.Logical', 'new KernelDeviceHandle(c->DeviceHandle)']);
requireText('SDK/src/NovaOryn.Kernel.Nvme/KernelNvme.cs', ['KernelDeviceBus.Logical', 'new KernelDeviceHandle(c->DeviceHandle)']);
requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', [
  "export type NovaOrynDeviceBus = 'platform' | 'pci' | 'usb' | 'acpi' | 'virtual' | 'logical';",
  'NovaOrynDeviceTreeSnapshot', 'inspectDeviceTree(projectPath: string)'
]);
requireText('packages/novaoryn-ide/src/browser/novaoryn-hardware-widget.tsx', [
  'this.projectService.inspectDeviceTree', 'snapshot.counts.pci', 'snapshot.counts.logical'
]);
const widget = read('packages/novaoryn-ide/src/browser/novaoryn-hardware-widget.tsx');
if (widget.includes('buildDeviceTree(')) throw new Error('IDE Hardware Tree still owns a parallel buildDeviceTree implementation.');
console.log('[ OK ] Unified device model: PCI, USB, ACPI, platform, virtual, logical; one hierarchical contract is used by kernel and IDE Hardware Tree.');
