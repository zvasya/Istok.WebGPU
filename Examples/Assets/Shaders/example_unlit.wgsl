struct PerFrameData {
    mvp: mat4x4<f32>,
    baseColor: vec4<f32>,
};

@group(0) @binding(0) var<uniform> perFrame: PerFrameData;
@group(0) @binding(1) var baseColorTexture: texture_2d<f32>;
@group(0) @binding(2) var baseColorSampler: sampler;

struct VertexOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) color: vec4<f32>,
    @location(1) uv: vec2<f32>,
};

@vertex
fn vs_main(@location(0) position: vec3<f32>, @location(1) color: vec4<f32>,
           @location(2) uv: vec2<f32>) -> VertexOutput {
    var output: VertexOutput;
    output.position = perFrame.mvp * vec4<f32>(position, 1.0);
    output.color = color * perFrame.baseColor;
    output.uv = uv;
    return output;
}

@fragment
fn fs_main(input: VertexOutput) -> @location(0) vec4<f32> {
    return textureSample(baseColorTexture, baseColorSampler, input.uv) * input.color;
}
