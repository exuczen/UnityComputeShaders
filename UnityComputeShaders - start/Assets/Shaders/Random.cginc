float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

/* Returns pseudo random number in range 0 <= x < 1 */
float random(float value, float seed = 0.546)
{
    float random = frac(sin(value + seed) * 143758.5453);
    return random;
}

float2 random2(float2 pt)
{
    return float2(
		random(pt.x, 3.9812),
		random(pt.y, 7.1536)
	);
}

float2 random2(float value)
{
    return float2(
		random(value, 3.9812),
		random(value, 7.1536)
	);
}

float3 random3(float value)
{
    return float3(
		random(value, 3.9812),
		random(value, 7.1536),
		random(value, 5.7241)
	);
}

float random(float2 pt, float seed)
{
    const float a = 12.9898;
    const float b = 78.233;
    const float c = 43758.543123;
    return frac(sin(dot(pt, float2(a, b)) + seed) * c);
}

float twiceRandom(float2 pt, float seed)
{
    const float a = 12.9898;
    const float b = 78.233;
    const float c = 43758.543123;
    return frac(sin(dot(random2(pt), float2(a, b)) + seed) * c);
}
