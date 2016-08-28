using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Usefull methods for Layermask
/// 
/// </summary>
/// <author>Bruno Mikoski</author>
public static class LayerExtensions
{
    /// <summary>
    /// Check if the game object fit the layer targetMask
    /// </summary>
    /// <param name="targetGameObject"></param>
    /// <param name="targetMask"></param>
    /// <returns></returns>
    public static bool IsInLayerMask(this GameObject targetGameObject, LayerMask targetMask)
    {
        return ((targetMask.value & (1 << targetGameObject.layer)) > 0);
    }

    /// <summary>
    /// Add the specific layers to the layer mask
    /// </summary>
    /// <param name="original"></param>
    /// <param name="targetLayerNames"></param>
    /// <returns></returns>
    public static LayerMask AddToMask(this LayerMask original, params string[] targetLayerNames)
    {
        return original | NamesToMask(targetLayerNames);
    }

    /// <summary>
    /// Generate one layermask based on the layers number
    /// </summary>
    /// <param name="layerNumbers"></param>
    /// <returns></returns>
    public static LayerMask LayerNumbersToMask(params int[] layerNumbers)
    {
        LayerMask ret = (LayerMask) 0;
        for (int i = 0; i < layerNumbers.Length; i++)
            ret |= (1 << layerNumbers[i]);

        return ret;
    }

    public static LayerMask NamesToMask(params string[] targetLayerNames)
    {
        LayerMask ret = (LayerMask) 0;
        for (int i = 0; i < targetLayerNames.Length; i++)
            ret |= (1 << LayerMask.NameToLayer(targetLayerNames[i]));

        return ret;
    }
}
