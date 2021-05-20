package com.microsoft.kiota.core.serialization;

import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;

import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.SerializationWriterFactory;

public class SerializationWriterFactoryRegistry implements SerializationWriterFactory {
    public HashMap<String, SerializationWriterFactory> contentTypeAssociatedFactories = new HashMap<>();

    @Override
    @Nonnull
    public SerializationWriter getSerializationWriter(@Nonnull final String contentType) {
        Objects.requireNonNull(contentType, "parameter contentType cannot be null");
        if(contentType.isEmpty()) {
            throw new NullPointerException("contentType cannot be empty");
        }
        if(contentTypeAssociatedFactories.containsKey(contentType)) {
            return contentTypeAssociatedFactories.get(contentType).getSerializationWriter(contentType);
        } else {
            throw new RuntimeException("Content type " + contentType + " does not have a factory to be serialized");
        }
    }
    
}
