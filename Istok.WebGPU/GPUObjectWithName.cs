namespace Istok.WebGPU;

public abstract unsafe class GPUObjectWithName<T>(T handle, string? label) : GPUObject<T>(handle) where T : unmanaged
{
	protected string? _label = label;
	public string Label
	{
		get => _label ?? string.Empty;
		set
		{
			_label = value;
			using (_label.ToWGPUStringView(out WGPUStringView stringView))
				SetLabel(stringView);
		}
	}
	
	protected abstract void SetLabel(WGPUStringView label);
}