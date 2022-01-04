import {
  InputDescriptor,
  RuntimeSelectListProviderSettings,
  SelectList,
  SelectListItem
} from "../models";
import {createElsaClient, ElsaApiClientProvider, ElsaClient} from "../services";
import {Container} from "typedi";

async function fetchRuntimeItems(options: RuntimeSelectListProviderSettings): Promise<SelectList> {
  const elsaClientProvider = Container.get(ElsaApiClientProvider);
  const elsaClient = await elsaClientProvider.getClient();
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
