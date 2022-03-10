const win = window as any;
const require = win.require;

export interface Monaco {
  editor: any;
  languages: any;
  KeyCode: any;
  Uri: any;
}

let isInitialized : boolean;

export function initializeMonacoWorker(libPath?: string): Promise<Monaco> {

  if (isInitialized)
    return win.monaco;

  const origin = document.location.origin;
  const baseUrl = libPath.startsWith('http') ? libPath : `${origin}/${libPath}`;

  require.config({paths: {'vs': `${baseUrl}/vs`}});
  win.MonacoEnvironment = {getWorkerUrl: () => proxy};

  let proxy = URL.createObjectURL(new Blob([`
	self.MonacoEnvironment = {
		baseUrl: '${baseUrl}'
	};
	importScripts('${baseUrl}/vs/base/worker/workerMain.js');
`], {type: 'text/javascript'}));

  return new Promise(resolve => {
    require(["vs/editor/editor.main"], () => {
      isInitialized = true;
      resolve(win.monaco);
    });
  });
}
