using System.Collections.Generic;
using Elsa.Testing.Shared;
using Xunit;

namespace Elsa.Models
{
    public class VariablesTests
    {
        [Theory(DisplayName = "The Remove method should remove a variable if such a variable exists"), AutoMoqData]
        public void Remove_removes_a_variable_by_name_if_present(string variableName, object variableData)
        {
            var sut = new Variables(new Dictionary<string,object> { { variableName, variableData } });

            sut.Remove(variableName);

            Assert.Empty(sut.Data);
        }

        [Theory(DisplayName = "The Remove method should not throw if used with a non-existent variable"), AutoMoqData]
        public void Remove_does_not_throw_when_trying_to_remove_a_variable_which_does_not_exist(string variableName)
        {
            var sut = new Variables();

            sut.Remove(variableName);

            // No assertion, if the line about doesn't throw then this test passed
        }

        [Theory(DisplayName = "The Remove method should support call-chaining by returning a self-reference"), AutoMoqData]
        public void Remove_returns_a_reference_to_itself(Variables sut, string variableName)
        {
            var result = sut.Remove(variableName);
            Assert.Same(sut, result);
        }

        [Theory(DisplayName = "The RemoveAll method should leave the variables collection empty"), AutoMoqData]
        public void RemoveAll_clears_all_variables(string variableName1,
                                                   string variableName2,
                                                   string variableName3,
                                                   object variableData)
        {
            var sut = new Variables(new Dictionary<string,object> {
                { variableName1, variableData },
                { variableName2, variableData },
                { variableName3, variableData },
            });
            
            sut.RemoveAll();

            Assert.Empty(sut.Data);
        }

        [Theory(DisplayName = "The RemoveAll method should support call-chaining by returning a self-reference"), AutoMoqData]
        public void RemoveAll_returns_a_reference_to_itself(Variables sut)
        {
            var result = sut.RemoveAll();
            Assert.Same(sut, result);
        }

    }
}