using System;
using System.Collections.Generic;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;

namespace Orleans.Serialization.Serializers
{
    /// <summary>
    /// Serializer for types which are abstract and therefore cannot be instantiated themselves, such as abstract classes and interface types.
    /// </summary>
    /// <typeparam name="TField"></typeparam>
    public sealed class AbstractTypeSerializer<TField> : IFieldCodec<TField> where TField : class
    {
        /// <inheritdoc/>
        void IFieldCodec<TField>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value)
        {
            // If the value is null then we will not be able to get its type in order to get a concrete codec for it.
            // Therefore write the null reference and exit.
            if (value is null)
            {
                _ = ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, null);
                return;
            }

            var fieldType = value.GetType();
            var specificSerializer = writer.Session.CodecProvider.GetCodec(fieldType);
            if (specificSerializer == null)
                ThrowSerializerNotFound(fieldType);

            specificSerializer.WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        /// <inheritdoc/>
        public TField ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<TField, TInput>(ref reader, field);
            }

            var fieldType = field.FieldType;
            if (fieldType is null)
            {
                ThrowMissingFieldType();
            }

            var specificSerializer = reader.Session.CodecProvider.GetCodec(fieldType);
            if (specificSerializer == null)
                ThrowSerializerNotFound(fieldType);

            return (TField)specificSerializer.ReadValue(ref reader, field);
        }

        private static void ThrowSerializerNotFound(Type type) => throw new KeyNotFoundException($"Could not find a serializer for type {type}.");

        private static void ThrowMissingFieldType() => throw new FieldTypeMissingException(typeof(TField));
    }
}