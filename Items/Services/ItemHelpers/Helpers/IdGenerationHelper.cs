using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace CommonLibExtended.Items.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class IdGenerationHelper
{
    public string GenerateMongoId()
    {
        return Generator.MongoIdGenerator.Generate();
    }

    public MongoId GenerateMongoObjectId()
    {
        return new MongoId(GenerateMongoId());
    }
}