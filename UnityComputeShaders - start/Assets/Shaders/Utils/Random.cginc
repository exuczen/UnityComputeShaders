#define XORSHIFT_RANGE_INV 1.0 / 4294967296.0

uint rng_state;

float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

/* http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/ */
uint wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
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

/* https://en.wikipedia.org/wiki/Linear_congruential_generator */
uint get_lgc(uint seed)
{
    return 1664525 * seed + 1013904223;
}

/* http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/ */
void set_rng_state(uint seed)
{
    rng_state = get_lgc(seed);
}

/* http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/ */
float random_xorshift()
{
	// Xorshift algorithm from George Marsaglia's paper
    rng_state ^= (rng_state << 13);
    rng_state ^= (rng_state >> 17);
    rng_state ^= (rng_state << 5);
    return rng_state * XORSHIFT_RANGE_INV;

}

float2 random2_xorshift()
{
    return float2(random_xorshift(), random_xorshift());
}

float3 randomUnitVector3()
{
    float3 v = float3(random_xorshift(), random_xorshift(), random_xorshift());
    return normalize(v - 0.5);
}

float3 randomMinMaxLengthVector3(float min, float max)
{
    float3 v = randomUnitVector3();
    float l = lerp(min, max, random_xorshift());
    return l * v;
}

// The noise function returns a value in the range -1.0f -> 1.0f 
float noise1(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);

    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;

    return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
					 lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
				lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
					 lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}
