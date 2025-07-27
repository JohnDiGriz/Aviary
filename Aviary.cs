using System;
using Roost;
using AviaryModules.Otherworlds;

public static class Aviary
{
    public static void Initialise()
    {
        try
        {
            CustomOtherworldsMaster.Enact();
        }
        catch (Exception ex)
        {
            Birdsong.TweetLoud(ex);
        }
    }
}