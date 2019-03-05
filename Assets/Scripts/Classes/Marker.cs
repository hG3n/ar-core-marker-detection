using UnityEngine;

public class Marker
{
    private int id;
    private int direction;
    private Vector2 tlScreen;
    private Vector2 trScreen;
    private Vector2 brScreen;
    private Vector2 blScreen;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Marker"/> class.
    /// </summary>
    /// <param name="id_in">Identifier in.</param>
    /// <param name="direction_in">Direction in.</param>
    /// <param name="tl_screen">Tl screen.</param>
    /// <param name="tr_screen">Tr screen.</param>
    /// <param name="br_screen">Br screen.</param>
    /// <param name="bl_screen">Bl screen.</param>
    public Marker(int id_in, int direction_in, Vector2 tl_screen, Vector2 tr_screen, Vector2 br_screen, Vector2 bl_screen)
    {
        id = id_in;
        direction = direction_in;
        tlScreen = tl_screen;
        trScreen = tr_screen;
        brScreen = br_screen;
        blScreen = bl_screen;
    }

    /// <summary>
    /// Gets the centroid.
    /// </summary>
    /// <returns>The centroid.</returns>
    public Vector2 GetCentroid()
    {
        float x_avg = (tlScreen.x + trScreen.x + brScreen.x + blScreen.x) /4 ;
        float y_avg = (tlScreen.y + trScreen.y + brScreen.y + blScreen.y) / 4;
        return new Vector2(x_avg, y_avg);
    }

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Marker"/>.
    /// </summary>
    /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Marker"/>.</returns>
    public override string ToString()
    {
        return "<- Marker | id: " + id + " ->";
    }


}
