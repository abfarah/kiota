import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { Parsable, SerializationWriterFactory } from "./serialization";

export class RequestInfo {
    public URI?: string;
    public httpMethod?: HttpMethod;
    public content?: ReadableStream;
    public queryParameters: Map<string, object> = new Map<string, object>();
    public headers: Map<string, string> = new Map<string, string>();
    private static jsonContentType = "application/json";
    private static binaryContentType = "application/octet-stream";
    private static contentTypeHeader = "Content-Type";
    public setJsonContentFromParsable = <T extends Parsable<T>>(value?: T | undefined, serializerFactory?: SerializationWriterFactory | undefined): void => {
        if(serializerFactory) {
            const writer = serializerFactory.getSerializationWriter(RequestInfo.jsonContentType);
            this.headers.set(RequestInfo.contentTypeHeader, RequestInfo.jsonContentType);
            writer.writeObjectValue(undefined, value);
            this.content = writer.getSerializedContent();
        }
    }
    public setStreamContent = (value: ReadableStream): void => {
        this.headers.set(RequestInfo.contentTypeHeader, RequestInfo.binaryContentType);
        this.content = value;
    }
    public setHeadersFromRawObject = (h: object) : void => {
        Object.entries(h).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
    public setQueryStringParametersFromRawObject = (q: object): void => {
        Object.entries(q).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
}