//#define THREADS_2D

#include "Utils/Random.cginc"
#include "Utils/Math.cginc"

struct Particle
{
    int2 position;
    float endTime;
    float4 color;
    uint randomSeed;
    bool active;
};

shared RWStructuredBuffer<Particle> particlesBuffer;
shared RWTexture2D<int> indexTexture;

int TexResolution;
uint PointsRowThreadsCount;
int2 CursorPosition;

Particle getClearParticle(uint randomSeed)
{
    Particle p;
    p.position = int2(-2, -2);
    p.endTime = 0.0;
    p.color = 0.0;
    p.randomSeed = randomSeed;
    p.active = false;
    return p;
}

#ifdef THREADS_2D
uint getParticleIndex(uint3 id)
{
    return id.y * PointsRowThreadsCount + id.x;
}
#else
uint getParticleIndex(uint3 id)
{
    return id.x;
}
#endif

int2 getParticlePosition(uint i)
{
    return particlesBuffer[i].position;
}

int getIndexFromTexture(int2 xy)
{
    return indexTexture[xy];
}

void setIndexInTexture(int2 xy, int i)
{
    indexTexture[xy] = i;
}

float getReciprocal(float a, float b, float x)
{
    return -1.0 / (a * x - b);
}

int2 getParticleRandomPosition(int i)
{
    rng_state = particlesBuffer[i].randomSeed;
    
    //int2 xy = (0.25 + random2_xorshift() * 0.5) * TexResolution;
    //int2 xy = (int2)(lerp(CircleRadius, TexResolution - CircleRadius, random2_xorshift()));
    int2 xy = (int2)(random2_xorshift() * TexResolution);
    
    particlesBuffer[i].randomSeed = rng_state;
    
    return xy;
}

int2 getParticleRandomPositionAroundCenter(int i, int2 center)
{
    rng_state = particlesBuffer[i].randomSeed;
    
    uint D = TexResolution;
    float angle = random_xorshift() * PI2;
    float r = random_xorshift();
    float a = 1.0;
    float b = 2.0;
    r = invLerp(getReciprocal(a, b, 0), getReciprocal(a, b, 1), getReciprocal(a, b, r));
    r *= (TexResolution >> 2);
    int2 xy = center + int2(r * cos(angle), r * sin(angle));
    xy = (xy + D) % D;
    
    particlesBuffer[i].randomSeed = rng_state;
    
    return xy;
}

void respawnParticle(int i, int2 xy, float lifetimeMin, float lifetimeMax, float time)
{
    rng_state = particlesBuffer[i].randomSeed;
    
    int j = getIndexFromTexture(xy);
    if (j >= 0 && i != j)
    {
        particlesBuffer[j].endTime = 0.0;
        particlesBuffer[j].active = false;
    }
    Particle p;
    p.position = xy;
    p.endTime = time + lerp(lifetimeMin, lifetimeMax, random_xorshift());
    p.color = float4(random3_xorshift(), 1.0);
    p.randomSeed = rng_state;
    p.active = true;
    
    particlesBuffer[i] = p;
}

void respawnParticle(int i, float lifetimeMin, float lifetimeMax, float time)
{
    int2 xy = getParticleRandomPosition(i);
    respawnParticle(i, xy, lifetimeMin, lifetimeMax, time);
}

void respawnParticleAroundCenter(int i, float lifetimeMin, float lifetimeMax, float time, int2 center)
{
    int2 xy = getParticleRandomPositionAroundCenter(i, center);
    respawnParticle(i, xy, lifetimeMin, lifetimeMax, time);
}
