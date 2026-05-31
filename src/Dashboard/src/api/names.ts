import {  Metadata } from '@/api/types.ts'

 class MetaInfoFactory {
  private meta: Metadata={entities:{}, protos:{}, products:{}, recipes:{}}
  private nameCache: Record<string, string> = {}

  public getProduct(id: string): { name: string; iconUrl: string }|undefined{
    return this.meta.products[id]
  }
  public getRecipe(id: string): { name: string; }|undefined{
    return this.meta.recipes[id]
  }
  public getProto(id: string):{name:string}|undefined {
    return this.meta.protos[id]
  }
  public getEntity(id: string):{name: string, protoId: string}|undefined{
    return this.meta.entities[id]
  }

  public getName(id: string): string|undefined{
    if( id.startsWith("proto")){
      return this.meta.protos[id]?.name
    }
    if(id.startsWith("product")){
      return this.meta.products[id]?.name
    }
    if(id.startsWith("recipe")){
      return this.meta.recipes[id]?.name
    }
    return this.meta.entities[id]?.name
  }

  public getCombinedName(id: string, otherId?: string) {
    if( !this.nameCache[id] ){
      const self = this.getName(id)!
      const other = otherId ? this.getName(otherId) : null

      this.nameCache[id] = other ? `${self} (${other})` : self
    }
    return this.nameCache[id]
  }

  public update(meta: Metadata) {
    if (meta === this.meta) {
      return
    }
    this.meta=meta;
    this.nameCache = {}
  }

}
export const MetaInfos = new MetaInfoFactory();

