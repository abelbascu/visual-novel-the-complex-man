shader_type canvas_item;

uniform float speed : hint_range(0.1, 2.0) = 0.5;
uniform float amplitude : hint_range(1.0, 10.0) = 3.0;
uniform float wave_width : hint_range(0.1, 1.0) = 0.5;

void fragment() {
    vec2 uv = UV;
    float time = TIME * speed;
    
    // Calculate the wave offset for each scan line
    float wave_offset = sin(uv.y * 10.0 + time) * amplitude * 0.001;
    
    // Apply the wave offset with a gradual phase shift
    uv.x += wave_offset * smoothstep(0.0, wave_width, fract(uv.y * 10.0));
    
    // Sample the texture with the modified UV coordinates
    COLOR = texture(TEXTURE, uv);
}