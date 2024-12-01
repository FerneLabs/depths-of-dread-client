// Generated by dojo-bindgen on Wed, 30 Oct 2024 17:53:29 +0000. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;

// Type definition for `dojo::model::layout::FieldLayout` struct
[Serializable]
public struct FieldLayout {
    public FieldElement selector;
    public Layout layout;
}

// Type definition for `core::byte_array::ByteArray` struct
[Serializable]
public struct ByteArray {
    public string[] data;
    public FieldElement pending_word;
    public uint pending_word_len;
}

// Type definition for `dojo::model::layout::Layout` enum
public abstract record Layout() : Enum {
    public record Fixed(byte[] value) : Layout;
    public record Struct(FieldLayout[] value) : Layout;
    public record Tuple(Layout[] value) : Layout;
    public record Array(Layout[] value) : Layout;
    public record ByteArray() : Layout;
    public record Enum(FieldLayout[] value) : Layout;
}

// Type definition for `core::option::Option::<core::integer::u32>` enum
public abstract record Option<A>() : Enum {
    public record Some(A value) : Option<A>;
    public record None() : Option<A>;
}


namespace depths_of_dread {
    // Model definition for `depths_of_dread::models::GameData` model
    public class GameData : ModelInstance {
        [ModelField("game_id")]
        public uint game_id;

        [ModelField("player")]
        public FieldElement player;

        [ModelField("floor_reached")]
        public ushort floor_reached;

        [ModelField("total_score")]
        public ushort total_score;

        [ModelField("start_time")]
        public ulong start_time;

        [ModelField("end_time")]
        public ulong end_time;

        // Start is called before the first frame update
        void Start() {
        }
    
        // Update is called once per frame
        void Update() {
        }
    }
}

        