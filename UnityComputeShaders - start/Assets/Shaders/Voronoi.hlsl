#define CENTER_COLOR float4(0.0, 0.0, 1.0, 1.0);
#define USE_XY_GRADIENT 1

struct Particle
{
    int2 position;
    float endTime;
    float4 color;
    uint randomSeed;
    float4 indexColor;
};

// Create a RenderTexture with enableRandomWrite flag and set it with cs.SetTexture
shared RWTexture2D<float4> outputTexture;
shared RWTexture2D<float4> indexTexture;
shared RWStructuredBuffer<Particle> particlesBuffer;
shared StructuredBuffer<float4> colorsBuffer;

float CircleRadiusF;

float4 getColor(int id)
{
    return colorsBuffer[id % colorsBuffer.Length];
}

float4 getXYGradientColor(int x, int y)
{
    return float4(abs(x) / CircleRadiusF, abs(y) / CircleRadiusF, 1.0, 1.0);
}

void plotParticleColors(int id)
{
    uint2 xy = particlesBuffer[id].position;
    
    indexTexture[xy] = particlesBuffer[id].indexColor;
#if USE_XY_GRADIENT
    outputTexture[xy] = CENTER_COLOR;
#else
    outputTexture[xy] = particlesBuffer[id].color;
#endif
}

void plot1(int x, int y, int2 c, float4 color, int id)
{
    uint2 xy = uint2(c.x + x, c.y + y);
    
    //if (outputTexture[xy].w == 0.0)
    if (indexTexture[xy].w == 0.0)
    {
        indexTexture[xy] = particlesBuffer[id].indexColor;
        outputTexture[xy] = color;
    }
}

void plot8(int x, int y, int2 center, int id)
{
    //float4 color = getColor(id);
    //float4 color = particlesBuffer[id].indexColor;
#if USE_XY_GRADIENT
    float4 color = getXYGradientColor(y, x);
#else
    float4 color = particlesBuffer[id].color;
#endif
    plot1(x, y, center, color, id);
    plot1(y, x, center, color, id);
    plot1(x, -y, center, color, id);
    plot1(y, -x, center, color, id);
    plot1(-x, -y, center, color, id);
    plot1(-y, -x, center, color, id);
    plot1(-x, y, center, color, id);
    plot1(-y, x, center, color, id);
}

void drawMidpointCircle(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    int d = 1 - r;

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
        plot8(x, y, c, id);

        y++;
    }
}

void drawMidpoint2Circle(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    int d = 1 - r;
    
    while (x > y)
    {
        plot8(x, y, c, id);
        
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
}

void drawJeskoCircle(int2 c, int r, int id)
{
    int t1 = r >> 4;
    int x = r;
    int y = 0;
    
    while (x >= y)
    {
        plot8(x, y, c, id);
        
        y++;
        t1 += y;
        int t2 = t1 - x;
        if (t2 >= 0)
        {
            t1 = t2;
            x--;
        }
    }
}

void drawHornCircle(int2 c, int r, int id)
{
    int d = -r;
    int x = r;
    int y = 0;
    
    while (y <= x)
    {
        plot8(x, y, c, id);
        
        d += (y << 1) + 1;
        y++;
        if (d > 0)
        {
            d += -(x << 1) + 2;
            x--;
        }
    }
}

void drawDiamond(int2 c, int r, int id)
{
    int x = r;
    int y = 0;
    
    while (x >= y)
    {
        plot8(x, y, c, id);
        
        y++;
        x--;
    }
}
