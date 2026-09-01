# Restling.Csv

Optional buffered CSV codec for Restling, powered by CsvHelper.

```csharp
using AMDevIT.Restling.Core;
using AMDevIT.Restling.Core.Network;
using AMDevIT.Restling.Core.Network.Builders;
using AMDevIT.Restling.Csv;

HttpClientContextBuilder builder = new();
builder.AddCodec(new CsvContentCodec());

RestlingClient client = new(builder);
RestRequestResult<List<Product>> result = await client.GetAsync<List<Product>>("https://api.example.com/products");
```

CSV request bodies require explicit codec selection:

```csharp
RestRequest<List<Product>> request = new("https://api.example.com/products",
                                         AMDevIT.Restling.Core.HttpMethod.Post,
                                         products)
{
    ContentMediaType = HttpMediaType.TextCsv,
    UseContentCodec = true
};
```

Use `CsvContentCodecOptions` to set culture, delimiter, header handling, CsvHelper configuration, maps, and converters. Response targets can be arrays, `List<T>`, or standard generic collection interfaces.
