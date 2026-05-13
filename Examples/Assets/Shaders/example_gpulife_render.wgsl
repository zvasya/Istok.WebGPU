
struct Uniforms {
    aspect: f32,
    mouse: vec4<f32>,
    size: f32
}

struct Camera {
    pos: vec2f,
    zoom: f32,
    notUsed: f32
}

struct Particle {
    @location(0) pos: vec2f,
    @location(1) vel: vec2f,
    @location(2) colour: f32
}

struct VertexOutput {
    @builtin(position) pos: vec4f,
    @location(0) uv: vec2f,
    @location(1) colour: f32,
}

@group(0) @binding(0) var<uniform> uniforms: Uniforms;
@group(0) @binding(1) var<uniform> camera: Camera;

@vertex
fn vertex(
    particle: Particle,
    @builtin(vertex_index) vertexIndex: u32
) -> VertexOutput {
    let a = uniforms.aspect;

    var positions = array<vec2f, 6>(
        vec2f(-1.0, -1.0), vec2f(1.0, -1.0), vec2f(-1.0, 1.0),
        vec2f(-1.0, 1.0), vec2f(1.0, -1.0), vec2f(1.0, 1.0)
    );

    let offset = positions[vertexIndex] * max(uniforms.size, 0.005 / camera.zoom);
    let worldPos = particle.pos + offset;

    var output: VertexOutput;
    output.pos = vec4f((worldPos.x - camera.pos.x) * camera.zoom / a, (worldPos.y - camera.pos.y) * camera.zoom, 0.0, 1.0);
    output.uv = positions[vertexIndex];
    output.colour = particle.colour;
    return output;
}

@group(0) @binding(2) var<storage, read> colours: array<f32>;

@fragment
fn fragment(@location(0) uv: vec2f, @location(1) colour: f32) -> @location(0) vec4f {
    let d = length(uv);
    if (d > 1) {
        discard;
    }
    let delta = fwidth(d);
    let alpha = 1.0 - smoothstep(1.0 - delta, 1.0, d);

    return vec4f(colours[u32(colour) * 3], colours[u32(colour) * 3 + 1], colours[u32(colour) * 3 + 2], alpha);
}