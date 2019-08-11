//public class to define ingredient, used to store ingredients in a List before writing in to JSON and after reading from JSON

using Godot;
using System;

public class Ingredient
{
    public string Amount { get; set; }
    public string Volume { get; set; }
    public string Ingredients { get; set; }

    public Ingredient(string _amount, string _volume, string _ingredient)
    {
        Amount = _amount;
        Volume = _volume;
        Ingredients = _ingredient;
    }

}
