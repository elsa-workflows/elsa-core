import {
  InputDescriptor,
  RuntimeSelectListProviderSettings,
  SelectList,
} from "../models";
import {Container} from "typedi";
import {ElsaClientProvider} from "../services";

async function fetchRuntimeItems(options: RuntimeSelectListProviderSettings): Promise<SelectList> {
  const elsaClientProvider = Container.get(ElsaClientProvider);
  const elsaClient = await elsaClientProvider.getElsaClient();
  return await elsaClient.designer.runtimeSelectListApi.get(options.runtimeSelectListProviderType, options.context || {});
}

export async function getSelectListItems(propertyDescriptor: InputDescriptor): Promise<SelectList> {
  const options: any = propertyDescriptor.options;
  let selectList: SelectList;

  if (!!options && options.runtimeSelectListProviderType)
    selectList = await fetchRuntimeItems(options);
  else if (Array.isArray(options))
    selectList = {
      items: options,
      isFlagsEnum: false
    };
  else
    selectList = options as SelectList;

  return selectList || {items: [], isFlagsEnum: false};
}
