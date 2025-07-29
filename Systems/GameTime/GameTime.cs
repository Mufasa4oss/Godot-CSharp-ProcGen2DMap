using System;
using Godot;

//no dependencies
public class GameTime
{
    public float tickToSecond(float tick)
    {
        float rval;
        rval = (int)(tick / 30);
        return rval;
    }
    public float tickToMinute(float tick) { return tickToSecond(tick / 60); }

    public float tickToHour(float tick) {return tickToMinute(tick / 3600); }
}
