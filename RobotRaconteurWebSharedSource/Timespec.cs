using System;

public class TimeSpec
{

    private static long start_ticks;
    private static long ticks_per_second;

    private static long start_seconds;
    private static int start_nanoseconds;

    private static DateTime start_time;
    private static bool started = false;

    private static bool iswindows = false;

    public long seconds;
    public int nanoseconds;

    private static object start_lock = new object();

    public TimeSpec(long seconds, int nanoseconds)
    {
        this.seconds = seconds;
        this.nanoseconds = nanoseconds;

        lock (start_lock)
        {
            if (!started) start();
        }

        cleanup_nanosecs();
    }

    public TimeSpec()
    {
        if (!started) start();
        this.seconds = 0;
        this.nanoseconds = 0;
    }

    public static TimeSpec Now
    {
        get
        {
            TimeSpec t = new TimeSpec();
            t.timespec_now();
            return t;
        }

    }

    private void timespec_now()
    {
        this.seconds = 0;
        this.nanoseconds = 0;

        lock (start_lock)
        {
            if (!started)
            {
                start();
                this.seconds = start_seconds;
                this.nanoseconds = start_nanoseconds;
            }
            else
            {
                if (iswindows)
                {
                    long ticks;
                    QueryPerformanceCounter(out ticks);

                    long diff_ticks = ticks - start_ticks;
                    long diff_secs = diff_ticks / ticks_per_second;

                    long diff_nanosecs = ((diff_ticks * (long)1e9) / ticks_per_second) % (long)1e9;



                    this.seconds = diff_secs + start_seconds;
                    this.nanoseconds = (int)diff_nanosecs + start_nanoseconds;
                }
                else
                {
                    TimeSpan t = DateTime.UtcNow.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                    this.seconds = (long)Math.Round(t.TotalSeconds);
                    this.nanoseconds = (int)Math.IEEERemainder(t.TotalMilliseconds * 1e6, 1e9);


                }

            }
        }

        cleanup_nanosecs();


    }


    private void start()
    {
        if (!started)
        {

#if !ROBOTRACONTEUR_BRIDGE
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.WinCE)
            {
                iswindows = true;
                QueryPerformanceCounter(out start_ticks);
                QueryPerformanceFrequency(out ticks_per_second);


            }
#endif

            start_time = DateTime.UtcNow.ToUniversalTime();
            TimeSpan t = start_time - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            start_seconds = (long)Math.Round(t.TotalSeconds);
            start_nanoseconds = (int)Math.IEEERemainder(t.TotalMilliseconds * 1e6, 1e9);

            started = true;
        }

    }




    /*[DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long lpFrequency);*/

    private static bool QueryPerformanceCounter(out long lpPerformanceCount)
    {
        lpPerformanceCount = System.Diagnostics.Stopwatch.GetTimestamp();
        return true;
    }

    private static bool QueryPerformanceFrequency(out long lpFrequency)
    {
        lpFrequency = System.Diagnostics.Stopwatch.Frequency;
        return true;
    }



    private void cleanup_nanosecs()
    {
        int nanoseconds1 = nanoseconds;

        int nano_div = nanoseconds / (int)(1e9);
        nanoseconds = nanoseconds % (int)(1e9);
        seconds += nano_div;



        if (seconds > 0 && nanoseconds < 0)
        {
            seconds = seconds - 1;
            nanoseconds = (int)1e9 + nanoseconds;
        }

        if (seconds < 0 && nanoseconds > 0)
        {
            seconds = seconds + 1;
            nanoseconds = nanoseconds - (int)1e9;
        }



    }

    public static bool operator ==(TimeSpec t1, TimeSpec t2)
    {
        if (((object)t1) == null && ((object)t2) == null) return true;
        if (((object)t1) == null || ((object)t2) == null) return false;

        return (t1.seconds == t2.seconds && t1.nanoseconds == t2.nanoseconds);
    }

    public static bool operator !=(TimeSpec t1, TimeSpec t2)
    {
        return !(t1 == t2);
    }

    public static TimeSpec operator -(TimeSpec t1, TimeSpec t2)
    {
        return new TimeSpec(t1.seconds - t2.seconds, t1.nanoseconds - t2.nanoseconds);
    }

    public static TimeSpec operator +(TimeSpec t1, TimeSpec t2)
    {
        return new TimeSpec(t1.seconds + t2.seconds, t1.nanoseconds + t2.nanoseconds);
    }

    public static bool operator >(TimeSpec t1, TimeSpec t2)
    {
        TimeSpec diff = t1 - t2;
        if (diff.seconds == 0) return diff.nanoseconds > 0;
        return diff.seconds > 0;
    }

    public static bool operator >=(TimeSpec t1, TimeSpec t2)
    {
        if (t1 == t2) return true;
        return t1 > t2;
    }

    public static bool operator <(TimeSpec t1, TimeSpec t2)
    {
        return t2 >= t1;
    }

    public static bool operator <=(TimeSpec t1, TimeSpec t2)
    {
        return t2 > t1;
    }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (!(obj is TimeSpec)) return false;
        return this == (TimeSpec)obj;
    }

    public override int GetHashCode()
    {
        return nanoseconds + (int)seconds;
    }
}