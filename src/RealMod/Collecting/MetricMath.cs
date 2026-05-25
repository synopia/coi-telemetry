using System;
using System.Collections.Generic;

namespace CoiTelemetry.RealMod.Collecting;


public static class MetricMath
{
    public static double Percent(int part, int total)=>total<=0 ? 0 : (double)part / total;

    public static Dictionary<T, double> Percent<T>(Dictionary<T,int> dict, int total) where T:Enum
    {
        Dictionary<T, double> result = new();
        foreach(var kvp in dict)
        {
            result[kvp.Key] = Percent(kvp.Value, total);
        }
        return result;
    }

    public static double PerMinute(double amount, double windowSeconds) => windowSeconds<=0 ? 0 : amount / windowSeconds * 60.0;

    public static double? EstimateMinutesUntilEmpty(double amount, double netPerMinute)
    {
        if (amount <= 0) return 0;
        if (netPerMinute >= 0) return null;
        return amount / -netPerMinute;
    }

    public static double? EstimateMinutesUntilFull(double amount,double capacity, double netPerMinute)
    {
        if(capacity<=0) return null;
        if(amount>=capacity) return 0;
        if(netPerMinute<=0) return null;
        return (capacity-amount)/netPerMinute;
    }
}

