using Godot;
using System;
using System.Runtime.Serialization;



public partial class ResourceItem
{
    //public int quantity { get; set; }
    public string name { get; set; }
    //public int rateOfAquistion { get; set; } //rate of aquisition per tick
    public ResourceItem(string name)
    {
        //this.quantity = num;
        this.name = name;
       // this.rateOfAquistion = roa;
    }
    public ResourceItem()
    {
    }
   
}