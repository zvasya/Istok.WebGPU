namespace Istok.WebGPU;

public readonly ref struct OptionalRef<T> where T : struct, allows ref struct
{
	private readonly bool hasValue; 
	internal readonly T value;

	public OptionalRef(T value)
	{
		this.value = value;
		hasValue = true;
	}

	public readonly bool HasValue => hasValue;

	public readonly T Value
	{
		get
		{
			if (!hasValue)
			{
				throw new InvalidOperationException("OptionalRef object not have a value.");
			}
			return value;
		}
	}

	public readonly T GetValueOrDefault() => value;

	public readonly T GetValueOrDefault(T defaultValue) =>
		hasValue ? value : defaultValue;

	public static implicit operator OptionalRef<T>(T value) =>
		new OptionalRef<T>(value);

	public static explicit operator T(OptionalRef<T> value) => value.Value;
}