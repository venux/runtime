﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Holds relevant state about a method parameter, like the default value of
    /// the parameter, and the position in the method's parameter list.
    /// </summary>
    [DebuggerDisplay("ParameterInfo={ParameterInfo}")]
    internal abstract class JsonParameterInfo
    {
        private Type _runtimePropertyType = null!;

        public abstract JsonConverter ConverterBase { get; }

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; protected set; }

        // The name from a Json value. This is cached for performance on first deserialize.
        public byte[]? JsonPropertyName { get; set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        public ParameterInfo ParameterInfo { get; private set; } = null!;

        // The name of the parameter as UTF-8 bytes.
        public byte[] ParameterName { get; private set; } = null!;

        // The name of the parameter.
        public string NameAsString { get; private set; } = null!;

        // Key for fast property name lookup.
        public ulong ParameterNameKey { get; private set; }

        // The zero-based position of the parameter in the formal parameter list.
        public int Position { get; private set; }

        private JsonClassInfo? _runtimeClassInfo;
        public JsonClassInfo RuntimeClassInfo
        {
            get
            {
                if (_runtimeClassInfo == null)
                {
                    _runtimeClassInfo = Options.GetOrAddClass(_runtimePropertyType);
                }

                return _runtimeClassInfo;
            }
        }

        public bool ShouldDeserialize { get; private set; }

        public virtual void Initialize(
            string matchingPropertyName,
            Type declaredPropertyType,
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            _runtimePropertyType = runtimePropertyType;

            Options = options;
            ParameterInfo = parameterInfo;
            Position = parameterInfo.Position;
            ShouldDeserialize = true;

            DetermineParameterName(matchingPropertyName);
        }

        private void DetermineParameterName(string matchingPropertyName)
        {
            NameAsString = matchingPropertyName;

            // `NameAsString` is valid UTF16, so just call the simple UTF16->UTF8 encoder.
            ParameterName = Encoding.UTF8.GetBytes(NameAsString);

            ParameterNameKey = JsonClassInfo.GetKey(ParameterName);
        }

        // Create a parameter that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(
            string matchingPropertyName,
            ParameterInfo parameterInfo,
            JsonSerializerOptions options)
        {
            JsonParameterInfo jsonParameterInfo = new JsonParameterInfo<sbyte>();
            jsonParameterInfo.Options = options;
            jsonParameterInfo.ParameterInfo = parameterInfo;
            jsonParameterInfo.ShouldDeserialize = false;

            jsonParameterInfo.DetermineParameterName(matchingPropertyName);

            return jsonParameterInfo;
        }

        public abstract bool ReadJson(ref ReadStack state, ref Utf8JsonReader reader, out object? argument);
    }
}
