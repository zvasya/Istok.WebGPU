namespace Istok.WebGPU;

public unsafe class GPUAdapter : GPUObject<WGPUAdapter>
{
	internal GPUAdapter(WGPUAdapter adapter) : base(adapter)
	{
	}

	public override void Dispose()
	{
		wgpuAdapterRelease(_handle);
	}

	WGPUFeatureName[]? _features;
	public WGPUFeatureName[] Features => _features ??= EnumerateFeatures();
	public GPUAdapterInfo Info => GetAdapterInfo();
	public WGPULimits Limits => GetLimits();
	

	WGPUFeatureName[] EnumerateFeatures()
	{

		WGPUSupportedFeatures wgpuSupportedFeatures = new WGPUSupportedFeatures();
		wgpuAdapterGetFeatures(_handle, &wgpuSupportedFeatures);
		WGPUFeatureName[] features = new WGPUFeatureName[wgpuSupportedFeatures.featureCount];
		Span<WGPUFeatureName> span = new Span<WGPUFeatureName>(wgpuSupportedFeatures.features, (int)wgpuSupportedFeatures.featureCount);
		span.CopyTo(features);
		wgpuSupportedFeaturesFreeMembers(wgpuSupportedFeatures);
		return features;
	}
	
	WGPULimits GetLimits()
	{
		var limits = new WGPULimits();
		wgpuAdapterGetLimits(_handle, &limits);
		return limits;
	}
	
	public GPUAdapterInfo GetAdapterInfo()
	{
		WGPUAdapterInfo adapterInfo = new WGPUAdapterInfo();
		wgpuAdapterGetInfo(_handle, &adapterInfo);
		
		// AdapterInfo adapterInfo = Requests.AdapterInfo.Request(this).Result;
		// WGPUAdapterInfo adapterInfo = new WGPUAdapterInfo();
		return new GPUAdapterInfo(adapterInfo);
	}

	Task<GPUDevice>? _requestDevice;
	public Task<GPUDevice> RequestDevice() => _requestDevice ??= Requests.Device.Request(this, new GPUDeviceDescriptor());
	public Task<GPUDevice> RequestDevice(GPUDeviceDescriptor deviceDescriptor) => _requestDevice ??= Requests.Device.Request(this, deviceDescriptor);
}