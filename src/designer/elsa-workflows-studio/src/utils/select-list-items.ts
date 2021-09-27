import {ActivityPropertyDescriptor, RuntimeSelectListItemsProviderSettings, SelectListItem} from "../models";
import {createElsaClient, ElsaClient} from "../services";

async function fetchRuntimeItems(serverUrl: string, options: RuntimeSelectListItemsProviderSettings): Promise<Array<SelectListItem>> {
  const elsaClient = await createElsaClient(serverUrl);
  return await elsaClient.designerApi.runtimeSelectItemsApi.get(options.runtimeSelectListItemsProviderType, options.context || {});
}

export async function getSelectListItems(serverUrl: string, propertyDescriptor: ActivityPropertyDescriptor): Promise<Array<SelectListItem>> {
  const options = propertyDescriptor.options;
  let items = [];

  if (!!options && options.runtimeSelectListItemsProviderType) {
    items = await fetchRuntimeItems(serverUrl, options);
  } else {
    items = options as Array<any> || [];
  }

  return items || [];
}
