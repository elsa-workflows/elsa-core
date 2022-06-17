// export function findParent(args: any): Array<any> {
//   const childNode = args.node;
//   const bbox = childNode.getBBox();
//
//   return this.getNodes().filter((node) => {
//
//     const data = node.getData();
//
//     if (data?.typeName == 'Elsa.If') {
//       debugger;
//       const targetBBox = node.getBBox();
//       return bbox.isIntersectWithRect(targetBBox);
//     }
//
//     return false;
//   })
// }
