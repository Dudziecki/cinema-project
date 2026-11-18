using System.ComponentModel.DataAnnotations;

namespace Cinema.Api.Endpoints;

internal static class EndpointValidation
{
    public static Dictionary<string, string[]> Validate<T>(T value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value!, new ValidationContext(value!), results, validateAllProperties: true);
        return results
            .SelectMany(result =>
            {
                var members = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
                return members.Select(member => new
                {
                    member,
                    message = result.ErrorMessage ?? "Некорректное значение."
                });
            })
            .GroupBy(x => x.member)
            .ToDictionary(x => x.Key, x => x.Select(y => y.message).ToArray());
    }
}
