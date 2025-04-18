using Godot;
using System;

public partial class ResourceItem : Node
{
    public int quantity;
    public string name;
    public int rateOfAquistion; //rate of aquisition per tick
    public ResourceItem(int num, string name, int roa)
    {
        this.quantity = num;
        this.name = name;
        this.rateOfAquistion = roa;
    }

    public override string ToString()
    {
        return $"{name}: {quantity}";
    }
}