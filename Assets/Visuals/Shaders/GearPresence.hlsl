#ifndef GEAR_PRESENCE_INCLUDED
#define GEAR_PRESENCE_INCLUDED

// The gear sits back in the background unless the player is picking an element, and lifts forward while they
// pick and for a moment after a switch (GearManager; the amounts are on GearArcArt.asset under Background).
// Everything the gear draws — arcs, hub, the active arc's pieces — goes through GearRecede, which takes away
// some of its opacity and some of its colour (toward grey).
//
// The arcs and hub read two globals. Both mean "how far receded", so 0 is fully present — which is also what
// an unset global reads as: nothing fades in a scene without a gear (the arc preview, say). The pieces are
// particles drawn with the shared cartoon particle shader, so they get the same two values per renderer
// instead (a property block, from ArcBits), leaving every other effect using that shader alone.

half _GearFadeOut;      // share of the opacity taken away
half _GearDesaturate;   // share of the colour taken away (toward grey)

half4 GearRecede(half4 color, half fadeOut, half desaturate)
{
    half grey = dot(color.rgb, half3(0.3, 0.59, 0.11));
    color.rgb = lerp(color.rgb, grey.xxx, desaturate);
    color.a *= 1.0 - fadeOut;
    return color;
}

half4 GearRecede(half4 color)
{
    return GearRecede(color, _GearFadeOut, _GearDesaturate);
}

#endif
