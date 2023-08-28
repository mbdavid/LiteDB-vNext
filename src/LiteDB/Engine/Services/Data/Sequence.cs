﻿namespace LiteDB.Engine;

internal struct Sequence
{
    public int LastInt;
    public long LastLong;

    public Sequence()
    {
        this.Reset();
    }

    public void Reset()
    {
        this.LastInt = int.MaxValue;
        this.LastLong = long.MaxValue;
    }

    public override string ToString() => Dump.Object(this);
}