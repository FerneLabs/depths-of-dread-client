// Generated by dojo-bindgen on Sun, 24 Nov 2024 19:07:25 +0000. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;
using BigInteger = System.Numerics.BigInteger;
using UnityEngine;

// Type definition for `depths_of_dread::models::Direction` enum
public abstract record Direction() : Enum {
    public record None() : Direction;
    public record Left() : Direction;
    public record Right() : Direction;
    public record Up() : Direction;
    public record Down() : Direction;

    public static Enum FromIndex(Type baseType, int index)
    {
        var nestedTypes = baseType.GetNestedTypes(BindingFlags.Public);
        if (index < 0 || index >= nestedTypes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        var type = nestedTypes.OrderBy(t => t.MetadataToken).ElementAt(index);
        return (Enum)Activator.CreateInstance(type);
    }

    public Vector3 ToVector3() {
        return this switch {
            None => Vector3.zero,
            Left => new Vector3(-1, 0, 0),
            Right => new Vector3(1, 0, 0),
            Up => new Vector3(0, 1, 0),
            Down => new Vector3(0, -1, 0),
            _ => throw new InvalidOperationException("Unknown direction")
        };
    }
}

// Model definition for `depths_of_dread::models::GameFloor` model
public class depths_of_dread_GameFloor : ModelInstance {
    [ModelField("game_id")]
        public uint game_id;

        [ModelField("size")]
        public Vec2 size;

        [ModelField("path")]
        public Direction[] path;

        [ModelField("end_tile")]
        public Vec2 end_tile;

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
    }
}

        