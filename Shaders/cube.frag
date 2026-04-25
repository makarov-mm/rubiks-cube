#version 400

in vec3 vWorldPos;
in vec3 vNormal;
in vec2 vUv;
in vec3 vLocalPos;

uniform vec3 uColor;
uniform vec3 uCameraPos;
uniform float uTime;
uniform int uMaterial;      // 0 = sticker, 1 = black plastic body, 2 = floor grid
uniform float uAlpha;
uniform int uReflection;

out vec4 fragColor;

float saturate(float x)
{
    return clamp(x, 0.0, 1.0);
}

float gridLine(vec2 p, float scale, float thickness)
{
    vec2 q = p * scale;
    vec2 grid = abs(fract(q - 0.5) - 0.5) / fwidth(q);
    float line = min(grid.x, grid.y);
    return 1.0 - saturate(line - thickness);
}

vec3 proceduralEnvironment(vec3 r)
{
    float top = saturate(r.y * 0.5 + 0.5);
    vec3 cold = vec3(0.015, 0.055, 0.11);
    vec3 blue = vec3(0.08, 0.48, 1.00);
    vec3 violet = vec3(0.35, 0.10, 0.90);
    float band = 0.5 + 0.5 * sin(8.0 * r.x + 4.0 * r.z + uTime * 0.55);
    return mix(cold, mix(blue, violet, band), top);
}

vec3 shadeSurface(vec3 baseColor, float roughness, float reflectionBoost)
{
    vec3 n = normalize(vNormal);
    vec3 v = normalize(uCameraPos - vWorldPos);

    vec3 l1 = normalize(vec3(-0.35, 0.90, 0.42));
    vec3 l2 = normalize(vec3( 0.70, 0.25, -0.60));

    float diff1 = max(dot(n, l1), 0.0);
    float diff2 = max(dot(n, l2), 0.0) * 0.38;

    vec3 h1 = normalize(l1 + v);
    vec3 h2 = normalize(l2 + v);

    float specPower = mix(95.0, 24.0, roughness);
    float spec1 = pow(max(dot(n, h1), 0.0), specPower);
    float spec2 = pow(max(dot(n, h2), 0.0), specPower * 0.75) * 0.45;

    float ndv = max(dot(n, v), 0.0);
    float fresnel = pow(1.0 - ndv, 4.0);
    float rim = pow(1.0 - ndv, 2.25);

    vec3 r = reflect(-v, n);
    vec3 env = proceduralEnvironment(r);

    vec3 ambient = baseColor * vec3(0.18, 0.20, 0.25);
    vec3 diffuse = baseColor * (diff1 + diff2) * 0.96;
    vec3 specular = (spec1 + spec2) * mix(vec3(0.9), env, 0.35 + reflectionBoost);
    vec3 reflected = env * fresnel * (0.22 + reflectionBoost);
    vec3 rimColor = vec3(0.06, 0.55, 1.0) * rim * 0.70;

    return ambient + diffuse + specular + reflected + rimColor;
}

void main()
{
    if (uMaterial == 2)
    {
        vec2 p = (vUv - vec2(0.5)) * 18.0;
        float minor = gridLine(p, 1.0, 0.65);
        float major = gridLine(p, 0.2, 0.85);
        float axisX = 1.0 - smoothstep(0.0, 0.025, abs(p.x));
        float axisZ = 1.0 - smoothstep(0.0, 0.025, abs(p.y));
        float fade = exp(-length(p) * 0.13);

        vec3 base = vec3(0.004, 0.007, 0.012);
        vec3 lineColor = vec3(0.02, 0.46, 0.95) * minor * 0.62;
        lineColor += vec3(0.18, 0.78, 1.00) * major * 1.05;
        lineColor += vec3(0.30, 0.95, 1.00) * max(axisX, axisZ) * 0.9;

        float glow = max(minor * 0.25, major * 0.55) * fade;
        vec3 color = base + lineColor * fade + vec3(0.01, 0.20, 0.42) * glow;
        fragColor = vec4(color, uAlpha * fade);
        return;
    }

    if (uMaterial == 1)
    {
        vec3 color = shadeSurface(vec3(0.006, 0.008, 0.012), 0.20, 0.75);
        float edge = max(max(abs(vLocalPos.x), abs(vLocalPos.y)), abs(vLocalPos.z));
        float bevelGlow = smoothstep(0.36, 0.50, edge);
        color *= 0.36 + bevelGlow * 0.82;
        if (uReflection == 1) color *= vec3(0.42, 0.70, 1.0);
        fragColor = vec4(color, uAlpha);
        return;
    }

    vec3 base = uColor;

    float micro = 0.5 + 0.5 * sin((vUv.x * 73.0 + vUv.y * 41.0) + sin(vUv.y * 37.0) * 0.8);
    base *= 0.96 + micro * 0.035;

    vec3 color = shadeSurface(base, 0.16, 0.30);

    float scan = smoothstep(0.020, 0.000, abs(fract(vWorldPos.y * 0.42 - uTime * 0.18) - 0.5));
    color += vec3(0.08, 0.55, 1.0) * scan * 0.20;

    vec2 edge = abs(vUv - 0.5) * 2.0;
    float edgeMask = smoothstep(0.78, 1.0, max(edge.x, edge.y));
    color *= 1.0 - edgeMask * 0.26;

    if (uReflection == 1)
        color *= vec3(0.38, 0.68, 1.0);

    fragColor = vec4(color, uAlpha);
}
