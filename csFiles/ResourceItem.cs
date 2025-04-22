using Godot;
using System;
using System.Runtime.Serialization;



public partial class ResourceItem : Resource
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
    public ResourceItem()
    {
    }
   
}