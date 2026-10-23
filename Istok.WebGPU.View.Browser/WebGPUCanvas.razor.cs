using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Silk.NET.Maths;

//This code is taken from https://github.com/carlfranklin/BlazorCanvas
namespace Istok.WebGPU.View;

public partial class WebGPUCanvas : ComponentBase, IAsyncDisposable, IWebGpuView
{
	Stopwatch sw = new Stopwatch();

	public Vector2D<int> Size { get; private set; }
	public Vector2D<int> FramebufferSize { get; private set; }

	public void SwapBuffers()
	{
	}

	public event Action<double>? Render;
	public event Action<Vector2D<int>>? FramebufferResize;


	/// <summary>
	/// JS Interop module used to call JavaScript
	/// </summary>
	private Lazy<Task<IJSObjectReference>> _moduleTask;

	/// <summary>
	/// Used to calculate the frames per second
	/// </summary>
	private DateTime _lastRender;

	private ElementReference _canvasRef;

	/// <summary>
	/// JS Runtime
	/// </summary>
	[Inject]
	protected IJSRuntime _jsRuntime { get; set; }

	/// <summary>
	/// Event called when the browser (and therefore the canvas) is resized
	/// </summary>
	[Parameter]
	public EventCallback<Vector2D<int>> CanvasResized { get; set; }

	/// <summary>
	/// Event called every time a frame can be redrawn
	/// </summary>
	[Parameter]
	public EventCallback<double> RenderFrame { get; set; }

	/// <summary>
	/// Event called on mouse down
	/// </summary>
	[Parameter]
	public EventCallback<CanvasMouseArgs> MouseDown { get; set; }

	/// <summary>
	/// Event called on mouse up
	/// </summary>
	[Parameter]
	public EventCallback<CanvasMouseArgs> MouseUp { get; set; }

	/// <summary>
	/// Event called on mouse move
	/// </summary>
	[Parameter]
	public EventCallback<CanvasMouseArgs> MouseMove { get; set; }


	/// <summary>
	/// Call this in your Blazor app's OnAfterRenderAsync method when firstRender is true
	/// </summary>
	/// <returns></returns>
	public async Task Initialize()
	{
		// We need to specify the .js file path relative to this code
		_moduleTask = new(() => _jsRuntime.InvokeAsync<IJSObjectReference>(
			"import", "./_content/Istok.WebGPU.View.Browser/WebGPUCanvas.razor.js").AsTask());


		// Load the module
		var module = await _moduleTask.Value;

		// Initialize
		await module.InvokeVoidAsync("initRenderJS", DotNetObjectReference.Create(this));

		// Dispose the module
		await module.DisposeAsync();
	}

	/// <summary>
	/// Handle the JavaScript event called when the browser/canvas is resized
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <returns></returns>
	[JSInvokable]
	public async Task ResizeInBlazor(int width, int height)
	{
		Size = new Vector2D<int>(width, height);
		FramebufferSize = Size;
		FramebufferResize?.Invoke(Size);
		// Raise the CanvasResized event to the Blazor app
		await CanvasResized.InvokeAsync(Size);
	}

	/// <summary>
	/// Handle the JavaScript event when a frame is ready to render
	/// </summary>
	/// <param name="timeStamp"></param>
	/// <returns></returns>
	[JSInvokable]
	public async ValueTask RenderInBlazor(float timeStamp)
	{
		double swElapsedMilliseconds = sw.Elapsed.TotalSeconds;
		Render?.Invoke(swElapsedMilliseconds);
		await RenderFrame.InvokeAsync(swElapsedMilliseconds);
		sw.Restart();
	}

	/// <summary>
	/// Handle the JavaScript window.mousedown event
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	[JSInvokable]
	public async Task OnMouseDown(CanvasMouseArgs args)
	{
		await MouseDown.InvokeAsync(args);
	}

	/// <summary>
	/// Handle the JavaScript window.mouseup event
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	[JSInvokable]
	public async Task OnMouseUp(CanvasMouseArgs args)
	{
		await MouseUp.InvokeAsync(args);
	}

	/// <summary>
	/// Handle the JavaScript window.mousemove event
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	[JSInvokable]
	public async Task OnMouseMove(CanvasMouseArgs args)
	{
		await MouseMove.InvokeAsync(args);
	}

	/// <summary>
	/// Dispose of our module resource
	/// </summary>
	/// <returns></returns>
	public async ValueTask DisposeAsync()
	{
		if (_moduleTask != null && _moduleTask.IsValueCreated)
		{
			var module = await _moduleTask.Value;
			await module.DisposeAsync();
		}
	}
}