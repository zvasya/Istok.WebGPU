namespace Istok.WebGPU;

public readonly struct GPUAdapterInfo(WGPUAdapterInfo adapterInfo)
{
	public readonly string Vendor = adapterInfo.vendor.ToStr();
	public readonly string Architecture = adapterInfo.architecture.ToStr();
	public readonly string Device = adapterInfo.device.ToStr();
	public readonly string Description = adapterInfo.description.ToStr();
	public readonly WGPUBackendType BackendType = adapterInfo.backendType;
	public readonly WGPUAdapterType AdapterType = adapterInfo.adapterType;
	
	public readonly uint VendorID = adapterInfo.vendorID;
	public readonly uint DeviceID = adapterInfo.deviceID;
	public readonly uint SubgroupMinSize = adapterInfo.subgroupMinSize;
	public readonly uint SubgroupMaxSize = adapterInfo.subgroupMaxSize;
}