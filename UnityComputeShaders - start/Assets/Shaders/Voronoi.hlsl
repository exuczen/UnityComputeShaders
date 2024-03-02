// Create a RenderTexture with enableRandomWrite flag and set it with cs.SetTexture
shared RWTexture2D<float4> output;
shared StructuredBuffer<float4> colorsBuffer;

float CircleRadiusF;

float4 getColor(int id)
{
    return colorsBuffer[id % colorsBuffer.Length];
}

float4 getCirclePixel(int x, int y)
{
    return float4(abs(x) / CircleRadiusF, abs(y) / CircleRadiusF, 1.0, 1.0);
}

void plot1(int x, int y, int2 c, float4 color)
{
    uint2 xy = uint2(c.x + x, c.y + y);
    if (output[xy].w == 0)
    {
        output[xy] = color;
    }
}

void plot8(int x, int y, int2 center, int colorID)
{
    //float4 color = getColor(colorID);
    float4 color = getCirclePixel(y, x);
    
    plot1(x, y, center, color);
    plot1(y, x, center, color);
    plot1(x, -y, center, color);
    plot1(y, -x, center, color);
    plot1(-x, -y, center, color);
    plot1(-y, -x, center, color);
    plot1(-x, y, center, color);
    plot1(-y, x, center, color);
}

void drawMidpointCircle(int2 c, int r, int colorID)
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
        plot8(x, y, c, colorID);

        y++;
    }
}

void drawMidpoint2Circle(int2 c, int r, int colorID)
{
    int x = r;
    int y = 0;
    int d = 1 - r;
    
    while (x > y)
    {
        plot8(x, y, c, colorID);
        
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

void drawJeskoCircle(int2 c, int r, int colorID)
{
    int t1 = r >> 4;
    int x = r;
    int y = 0;
    
    while (x >= y)
    {
        plot8(x, y, c, colorID);
        
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

void drawHornCircle(int2 c, int r, int colorID)
{
    int d = -r;
    int x = r;
    int y = 0;
    
    while (y <= x)
    {
        plot8(x, y, c, colorID);
        
        d += (y << 1) + 1;
        y++;
        if (d > 0)
        {
            d += -(x << 1) + 2;
            x--;
        }
    }
}

void drawDiamond(int2 c, int r, int colorID)
{
    int x = r;
    int y = 0;
    
    while (x >= y)
    {
        plot8(x, y, c, colorID);
        
        y++;
        x--;
    }
}
