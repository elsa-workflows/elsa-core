using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.Core.Activities;


[Activity("NexxLogic",
    Category = "Registries",
    Description = "Gets the registry definitions based on the registry type and definition keys.",
    DisplayName = "Get Registry Definitions By Keys",
    Kind = ActivityKind.Task)]
[FlowNode(ActivityOutcomes.SUCCESS, ActivityOutcomes.FAIL)]
public class GetRegistryDefinitionsByKeys : Activity
{
    [Required]
    [Input(
        Description = "The definition type you are interested in.",
        Options = new[]
        {
            RegistryDefinitionType.Schema,
            RegistryDefinitionType.Transformation,
            RegistryDefinitionType.HttpConnector,
            RegistryDefinitionType.GraphQLQuery
        },
        DefaultValue = RegistryDefinitionType.Schema,
        UIHint = InputUIHints.Dropdown
    )]
    public Input<RegistryDefinitionType> DefinitionType { get; set; } = new(RegistryDefinitionType.Schema);

    [Required]
    [Input(Description = "Definition keys.", UIHint = InputUIHints.MultiText)]
    public Input<IEnumerable<string>?> DefinitionKeys { get; set; } = default!;

    public Output<object?> Definitions { get; set; } = default!;
    public Output<ActivityResult> ActivityResult { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<GetRegistryDefinitionsByKeys>>();

        var definitionKeys = context.Get(DefinitionKeys);
        context.JournalData.Add(nameof(DefinitionKeys), definitionKeys);

        var definitionType = context.Get(DefinitionType);
        context.JournalData.Add(nameof(DefinitionType), definitionType);

        var registriesDictionary = GetRegistriesDictionary(context);
        var definitionsOfSelectedType = registriesDictionary[definitionType].ToList();



        var result = new JObject();
        var keysNotFound = new List<string>();
        var duplicateKeys = new List<string>();
        foreach (var key in definitionKeys?.Distinct() ?? Array.Empty<string>())
        {
            var definitionsMatchingKey = definitionsOfSelectedType
                .Where(x => x.Key == key).ToList();

            if (!definitionsMatchingKey.Any())
            {
                keysNotFound.Add(key);
            }
            else if (definitionsMatchingKey.Count() > 1)
            {
                duplicateKeys.Add(definitionsMatchingKey.First().Key);
            }
            else
            {
                result[key] = definitionsMatchingKey.First().Definition;
            }
        }

        if (keysNotFound.Any())
        {
            await HandleInvalidDefinitionsError(context, logger, keysNotFound, "Could not found definitions");
            return;
        }

        if (duplicateKeys.Any())
        {
            await HandleInvalidDefinitionsError(context, logger, duplicateKeys, "Found duplicate definitions");
            return;
        }

        await HandleSuccess(context, result);
    }

    private async Task HandleSuccess(
        ActivityExecutionContext context,
        JObject result)
    {
        var activityResult = new ActivityResult();

        context.JournalData.Add(nameof(ActivityResult), activityResult);
        context.Set(ActivityResult, activityResult);

        var jsonValue = JsonValue.Parse(result.ToString());
        var jsonObject = jsonValue as JsonObject;

        context.JournalData.Add(nameof(Definitions), jsonObject);
        context.Set(Definitions, jsonObject);

        await context.CompleteActivityWithOutcomesAsync(ActivityOutcomes.SUCCESS);
    }

    private async Task HandleInvalidDefinitionsError(
        ActivityExecutionContext context,
        ILogger<GetRegistryDefinitionsByKeys> logger,
        List<string> invalidDefinitions,
        string errorMessage)
    {
        var activityResult = new ActivityResult();

        logger.LogWarning(
            "Failed to execute activity '{activity}'. {errorMessage} '{definitions}'",
            GetType().Name,
            errorMessage,
            string.Join(", ", invalidDefinitions));

        activityResult.ErrorMessage = $"{errorMessage} '{string.Join(", ", invalidDefinitions)}'";
        activityResult.IsSuccessful = false;

        context.JournalData.Add(nameof(ActivityResult), activityResult);
        context.Set(ActivityResult, activityResult);

        await context.CompleteActivityWithOutcomesAsync(ActivityOutcomes.FAIL);
    }

    private static IDictionary<RegistryDefinitionType, IEnumerable<RegistryDefinition>> GetRegistriesDictionary(
        ActivityExecutionContext context)
    {
        var options = context
            .GetRequiredService<IOptionsMonitor<ConfigurationWithMetadata<IEnumerable<RegistryDefinition>>>>();

        //var json = "{\"script\":[{\"path\":\"$.INPUT_CALCS.MP_POLICY\",\"value\":[],\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*]\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*]\",\"value\":\"=partial(@.sequenceNumber)\",\"command\":\"set\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].coverage\",\"command\":\"remove\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].sequenceNumber\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].RS_REQUEST_ID\",\"command\":\"move\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].RS_REQUEST_ID\",\"value\":\"=parse($.INPUT_CALCS.MP_POLICY[*].RS_REQUEST_ID)\",\"command\":\"set\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].RS_CALCULATION_DT\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].effectiveChangeDate\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].RS_CALCULATION_TYPE\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].premiumCalculationMethod\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].RS_MUTATION_DT\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].financialInformation.calculationDate\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_POLICY_ID\",\"value\":\"$.policyRequest.masterAgreement[*].contractNumber\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_POLICY_START_DT\",\"value\":\"=parse($.policyRequest.masterAgreement[*].policy[*].effectiveDate)\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_POLICY_END_DT\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].expiryDate\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_STATUS_POLICY_KEY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].statusTypeExplanation\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_PREMIUM_FRQ\",\"value\":\"=parse($.policyRequest.masterAgreement[*].policy[*].paymentTermInMonths)\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_TERM_PREMIUM_POLICY_PK\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].totalInstallmentAmount\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_TERM_PREMIUM_POLICY_PK_OLD\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].MP_TERM_PREMIUM_POLICY_PK_OLD\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_SURRENDER_POLICY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].financialInformation.premiumFreeSurrenderAmount\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_SURRENDER_POLICY_OLD\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].MP_SURRENDER_POLICY_OLD\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_PUP_VALUE_POLICY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].financialInformation.guaranteedNonContributoryValue\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_PUP_VALUE_POLICY_OLD\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].MP_PUP_VALUE_POLICY_OLD\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_RESERVE_POLICY_OLD\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].MP_RESERVE_POLICY_OLD\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_RESERVE_PRIMO_POLICY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].financialInformation.MP_RESERVE_PRIMO_POLICY\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_RESERVE_ULTIMO_POLICY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].financialInformation.MP_RESERVE_ULTIMO_POLICY\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COUNTRY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].country\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_CHANNEL\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].distributionType\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].coverage[*]\",\"command\":\"add\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].refKey\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_COVERAGE_ID\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].productVersionNumber\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_PRODUCT_KEY\",\"command\":\"move\"},{\"path\":\"$.INPUT_CALCS.insured.refKey\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].party[?(@.entityType == 'insuredPerson')].partyRef[0]\",\"description\":\"What when partyRef is array?\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.insured.birthDay\",\"value\":\"$.policyRequest.masterAgreement[*].party[?(@.refKey == $.INPUT_CALCS.insured.refKey)].birthDate\",\"command\":\"add\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_DATE_OF_BIRTH_1_DT\",\"frompath\":\"$.INPUT_CALCS.insured.birthDay\",\"command\":\"copy\"},{\"path\":\"$.INPUT_CALCS.insured.gender\",\"value\":\"$.policyRequest.masterAgreement[*].party[?(@.refKey == $.INPUT_CALCS.insured.refKey)].gender\",\"command\":\"add\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_GENDER_1\",\"frompath\":\"$.INPUT_CALCS.insured.gender\",\"command\":\"copy\"},{\"path\":\"$.INPUT_CALCS.partner.refKey\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].party[?(@.entityType == 'partner')].partyRef[0]\",\"description\":\"When partyRef is array?\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.partner.birthDay\",\"value\":\"$.policyRequest.masterAgreement[*].party[?(@.refKey == $.INPUT_CALCS.partner.refKey)].birthDate\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.partner.gender\",\"value\":\"$.policyRequest.masterAgreement[*].party[?(@.refKey == $.INPUT_CALCS.partner.refKey)].gender\",\"command\":\"add\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_DATE_OF_BIRTH_2_DT\",\"frompath\":\"$.INPUT_CALCS.partner.birthDay\",\"command\":\"copy\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_GENDER_2\",\"frompath\":\"$.INPUT_CALCS.partner.gender\",\"command\":\"copy\"},{\"path\":\"$.INPUT_CALCS.partner\",\"command\":\"remove\"},{\"path\":\"$.INPUT_CALCS.insured\",\"command\":\"remove\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_COUNTRY\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].country\",\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_CHANNEL\",\"value\":\"$.policyRequest.masterAgreement[*].policy[*].distributionType\",\"command\":\"add\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].insuredCapital\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_BENEFITS_COVERAGE\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].insuredCapitalPremium\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_PREMIUM_PAYING_BENEFITS\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].insuredCapitalInterestSharing\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_BENEFITS_PRF_RSK\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].insuredCapitalInterestSharingDiscount\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_BENEFITS_PRF_OVR\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].insuredCapitalDelta\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].RS_MUTATION_BENEFITS\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].grossPremiumInstallment\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_GROSS_PREM_COVERAGE_PK\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].annualGrossPremiumAmount\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_GROSS_PREM_COVERAGE_PJ\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].appliedCorrectionAmount\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_GROSS_PREM_COVERAGE_ADJUST_PJ\",\"command\":\"move\"},{\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].totalInstallmentAmount\",\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_TERM_PREM_COVERAGE_PK\",\"command\":\"move\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_RESERVE_PRIMO_COVERAGE\",\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].financialInformation.MP_RESERVE_PRIMO_COVERAGE\",\"command\":\"move\"},{\"topath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_RESERVE_ULTIMO_COVERAGE\",\"frompath\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].financialInformation.MP_RESERVE_ULTIMO_COVERAGE\",\"command\":\"move\"},{\"path\":\"$.INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*]\",\"value\":\"=partial(@.MP_COVERAGE_ID,@.MP_PRODUCT_KEY,@.MP_DATE_OF_BIRTH_1_DT,@.MP_DATE_OF_BIRTH_2_DT,@.MP_GENDER_1,@.MP_GENDER_2,@.MP_COUNTRY,@.MP_CHANNEL,@.MP_BENEFITS_COVERAGE,@.MP_PREMIUM_PAYING_BENEFITS,@.MP_BENEFITS_PRF_RSK,@.MP_BENEFITS_PRF_OVR,@.RS_MUTATION_BENEFITS,@.MP_GROSS_PREM_COVERAGE_PK,@.MP_GROSS_PREM_COVERAGE_PJ,@.P_GROSS_PREM_COVERAGE_PJ_OLD,@.MP_GROSS_PREM_COVERAGE_ADJUST_PJ,@.MP_TERM_PREM_COVERAGE_PK,@.MP_TERM_PREM_COVERAGE_PK_OLD,@.MP_CST_LOAD_DISTRI_PJ,@.MP_CST_LOAD_POL_CHARGE_PJ,@.MP_CST_LOAD_KIDX_COVERAGE_PJ,@.MP_CST_LOAD_RISKADJ_PJ,@.MP_CST_LOAD_EMPLOYEE_PJ,@.MP_CST_LOAD_PVA_PJ,@.MP_CST_LOAD_CARENZ_PJ,@.MP_RESERVE_PRIMO_COVERAGE,@.MP_RESERVE_ULTIMO_COVERAGE,@.MP_PREMIUM_END_DT,@.MP_SURRENDER_COVERAGE,@.MP_PUP_VALUE_COVERAGE)\",\"command\":\"set\"},{\"path\":\"$.INPUT_CALCS.newCalculation\",\"value\":[],\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.newCalculation\",\"description\":\"default empty values when CALC_NEW \",\"value\":{\"MP_COVERAGE_LAYER_ID\":\"101001\",\"MP_MAIN_YN\":\"TRUE\",\"MP_STATUS_KEY\":\"STATUS_prm\",\"MP_PREMIUM_START_DT\":\"2020-04-07\",\"MP_PREMIUM_END_DT\":\"9999-12-31\",\"MP_AGE_CORR_FISCAL_1_YRS\":0,\"MP_AGE_CORR_FISCAL_2_YRS\":null,\"MP_COSTS_ID\":null,\"MP_MORTALITY_FISCAL\":null,\"MP_MORTALITY_COMMERCIAL\":null,\"MP_INTEREST_PRC\":null,\"MP_BENEFITS\":7000,\"MP_PREMIUM_PAYING_BENEFITS\":7000,\"MP_RESERVE_PRIMO\":0,\"MP_RESERVE_ULTIMO\":0,\"MP_SURRENDER\":0,\"MP_PUP_VALUE\":0,\"MP_GROSS_PREM_PK\":0,\"MP_NET_PREM_FISCAL_PK\":0,\"MP_NET_PREM_COMM_PK\":0,\"MP_ZILLMER_PREM_PK\":0,\"MP_CST_LOAD_KIDX_PJ\":0,\"MP_FISCAL_RESERVE\":0,\"MP_COMMERCIAL_RESERVE\":0,\"MP_GROSS_PREM_PJ_OLD\":0,\"MP_GROSS_PREM_PJ\":0,\"MP_NET_PREM_PJ_OLD\":0,\"MP_NET_PREM_PJ\":0,\"MP_COMM_PREM_PJ_OLD\":0,\"MP_ZILLMER_PREM_PJ_OLD\":0},\"command\":\"add\"},{\"path\":\"$.INPUT_CALCS.newCalculation[*].newCalculation\",\"value\":\"$.INPUT_CALCS.MP_POLICY[?(@.RS_CALCULATION_TYPE == 'CALC_NEW')].RS_CALCULATION_TYPE\",\"command\":\"add\"},{\"Path\":\"$.INPUT_CALCS.newCalculation..[?($.INPUT_CALCS.newCalculation[*].newCalculation == null )]\",\"command\":\"remove\"},{\"Path\":\"$.INPUT_CALCS.newCalculation[*].newCalculation\",\"command\":\"remove\"},{\"fromPath\":\"$.INPUT_CALCS.newCalculation\",\"topath\":\".INPUT_CALCS.MP_POLICY[*].MP_COVERAGE[*].MP_COVERAGE_LAYER\",\"command\":\"copy\"},{\"Path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"remove\"}]}";

        var json = "{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"description\":\"default empty values when CALC_NEW \",\"value\":{\"MP_COVERAGE_LAYER_ID\":\"101001\",\"MP_MAIN_YN\":\"TRUE\",\"MP_STATUS_KEY\":\"STATUS_prm\",\"MP_PREMIUM_START_DT\":\"2020-04-07\",\"MP_PREMIUM_END_DT\":\"9999-12-31\",\"MP_AGE_CORR_FISCAL_1_YRS\":0,\"MP_AGE_CORR_FISCAL_2_YRS\":null,\"MP_COSTS_ID\":null,\"MP_MORTALITY_FISCAL\":null,\"MP_MORTALITY_COMMERCIAL\":null,\"MP_INTEREST_PRC\":null,\"MP_BENEFITS\":7000,\"MP_PREMIUM_PAYING_BENEFITS\":7000,\"MP_RESERVE_PRIMO\":0},\"command\":\"add\"}]}";


        var token = JToken.Parse(json);

        var registriesDictionary = new Dictionary<RegistryDefinitionType, IEnumerable<RegistryDefinition>>()
        {
            {
                RegistryDefinitionType.Schema,
                options.Get(ActivityConfigurationsConstants.SCHEMA_DEFINITIONS).ProcessedSetting
            },
            {
                RegistryDefinitionType.Transformation,
                new List<RegistryDefinition>{ new RegistryDefinition("premiumCalculationAfdToIipEngine", token) }
            },
            {
                RegistryDefinitionType.HttpConnector,
                options.Get(ActivityConfigurationsConstants.HTTP_CONNECTOR_DEFINITIONS).ProcessedSetting
            },
            {
                RegistryDefinitionType.GraphQLQuery,
                options.Get(ActivityConfigurationsConstants.GRAPHQL_QUERIES).ProcessedSetting
            }
        };

        return registriesDictionary;
    }

}


public class ActivityResult
{
    public bool IsSuccessful { get; set; } = true;
    public string ErrorMessage { get; set; } = default!;
}

public record RegistryDefinition(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("definition")]
    JToken Definition);

public static class ActivityConfigurationsConstants
{
    public const string TRANSFORMATION_DEFINITIONS = "TransformationDefinitions";
    public const string SCHEMA_DEFINITIONS = "SchemaDefinitions";
    public const string HTTP_CONNECTOR_DEFINITIONS = "HttpConnectorDefinitions";
    public const string GRAPHQL_QUERIES = "GraphQLQueries";
}

public class ConfigurationWithMetadata<TResult>
{
    public string Data { get; set; }
    public Metadata Metadata { get; set; }
    public TResult ProcessedSetting { get; set; }
}

public class Metadata
{
    public IEnumerable<string> AppliedOperationsSequence { get; set; }
    public IDictionary<string, string> Operations { get; set; }
}

public enum RegistryDefinitionType
{
    Schema,
    Transformation,
    HttpConnector,
    GraphQLQuery
}


public static class ActivityOutcomes
{
    public const string SUCCESS = "Success";
    public const string FAIL = "Fail";
}

