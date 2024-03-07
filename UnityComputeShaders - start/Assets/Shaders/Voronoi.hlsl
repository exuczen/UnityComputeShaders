﻿//#define CENTER_COLOR float4(1.0, 1.0, 1.0, 1.0);
#define USE_PARTICLE_COLOR 1

struct Particle
{
    int2 position;
    float endTime;
    float4 color;
    uint randomSeed;
    bool filled;
};

// Create a RenderTexture with enableRandomWrite flag and set it with cs.SetTexture
shared RWTexture2D<float4> outputTexture;
shared RWStructuredBuffer<Particle> particlesBuffer;
shared Buffer<float4> colorsBuffer;
shared RWBuffer<int> indexBuffer;
shared RWBuffer<int> tempBuffer;

int TexResolution;
float CircleRadiusF;

Particle getClearParticle(uint randomSeed)
{
    Particle p;
    p.position = int2(-1, -1);
    p.endTime = 0.0;
    p.color = 0.0;
    p.randomSeed = randomSeed;
    p.filled = false;
    return p;
}

float4 getColor(int id)
{
    return colorsBuffer[id % colorsBuffer.Length];
}

float4 getXYGradientColor(int x, int y)
{
    return float4(abs(x) / CircleRadiusF, abs(y) / CircleRadiusF, 1.0, 1.0);
}

void plotParticle(Particle p, int i)
{
    if (p.position.x < 0)
    {
        return;
    }
    int2 xy = p.position;
    
    indexBuffer[xy.y * TexResolution + xy.x] = i;
#ifdef CENTER_COLOR
    outputTexture[xy] = CENTER_COLOR;
#elif USE_PARTICLE_COLOR
    outputTexture[xy] = p.color;
#else
    outputTexture[xy] = float4(0.0, 0.0, 1.0, 1.0);
#endif
}

void plotParticle(int id)
{
    plotParticle(particlesBuffer[id], id);
}

bool plot1(int x, int y, int2 c, float4 color, int id)
{
    x += c.x;
    y += c.y;
    int2 xy = int2(x, y);
    bool inBounds = x >= 0 && x < TexResolution && y >= 0 && y < TexResolution;
    bool result = inBounds && outputTexture[xy].w == 0.0;
    //bool result = inBounds && indexBuffer[y * TexResolution + x] < 0;
    if (result)
    {
        indexBuffer[y * TexResolution + x] = id;
        outputTexture[xy] = color;
    }
    return result;
}

bool plot8(int x, int y, int2 center, int id)
{
    //float4 color = getColor(id);
#if USE_PARTICLE_COLOR
    float4 color = particlesBuffer[id].color;
#else
    float4 color = getXYGradientColor(y, x);
#endif
    bool result = false;
    result = result | plot1(x, y, center, color, id);
    result = result | plot1(y, x, center, color, id);
    result = result | plot1(x, -y, center, color, id);
    result = result | plot1(y, -x, center, color, id);
    result = result | plot1(-x, -y, center, color, id);
    result = result | plot1(-y, -x, center, color, id);
    result = result | plot1(-x, y, center, color, id);
    result = result | plot1(-y, x, center, color, id);
    return result;
}

bool drawMidpointCircle(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    int d = 1 - r;
    bool result = false;

    while (x >= y)
    {
        if (d < 0)
        {
            d += (y << 1) + 3;
        }
        else
        {
            d += ((y - x) << 1) + 5;
            x--;
        }
        result = result | plot8(x, y, c, id);

        y++;
    }
    return result;
}

bool drawMidpoint2Circle(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    int d = 1 - r;
    bool result = false;
    
    while (x > y)
    {
        result = result | plot8(x, y, c, id);
        
        y++;
        if (d <= 0)
        {
            d += (y << 1) + 1;
        }
        else
        {
            x--;
            d += ((y - x) << 1) + 1;
        }
    }
    return result;
}

bool drawJeskoCircle(int2 c, int r, int id)
{
    int t1 = r >> 4;
    int x = r;
    int y = 0;
    bool result = false;
    
    while (x >= y)
    {
        result = result | plot8(x, y, c, id);
        
        y++;
        t1 += y;
        int t2 = t1 - x;
        if (t2 >= 0)
        {
            t1 = t2;
            x--;
        }
    }
    return result;
}

bool drawHornCircle(int2 c, int r, int id)
{
    int d = -r;
    int x = r;
    int y = 0;
    bool result = false;
    
    while (y <= x)
    {
        result = result | plot8(x, y, c, id);
        
        d += (y << 1) + 1;
        y++;
        if (d > 0)
        {
            d += -(x << 1) + 2;
            x--;
        }
    }
    return result;
}

bool drawDiamond(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    bool result = false;
    
    while (x >= y)
    {
        result = result | plot8(x, y, c, id);
        
        y++;
        x--;
    }
    return result;
}
