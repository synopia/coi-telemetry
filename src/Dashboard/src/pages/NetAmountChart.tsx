import { ExportSummary} from '@/api/types.ts'
import EChartsReact from 'echarts-for-react'
import { MetaInfos } from '@/api/names.ts'

const richKey = (productId: string)=>{
  return productId.replace(/[^a-zA-Z0-9_]/g, "_")
}
const makeProductRichStyles = (productIds: string[])=>{
  const rich: Record<string, any>={
    name:{
      fontSize:12,
      padding:[0,0,0,0],
      align:"left"
    }
  }
  for(const productId of productIds){
    const meta = MetaInfos.getProduct(productId)
    if(!meta?.iconUrl){
      continue
    }

    rich[richKey(productId)] = {
      height: 18,
      width: 18,
      align: 'center',
      backgroundColor: {
        image: `http://localhost:17891/${meta.iconUrl}`,
      },
    }
  }
  return rich
}

export const NetAmountChart = ({ summary, maxProducts, type }: { summary: ExportSummary, maxProducts: number, type:"asc"|"desc" }) => {
  const sorted =
    type === 'desc'
      ? summary.productFlow.sort((a, b) => b.netPerMinute - a.netPerMinute)
      : summary.productFlow.sort((a, b) => a.netPerMinute - b.netPerMinute)

  const data = sorted
    .slice(0, maxProducts)
  if(type==="asc"){
    data.reverse()
  }

  const productIds = data.map((row) => row.productId)

  const option = {
    xAxis: {
      type: 'value',
    },
    yAxis: {
      type: 'category',
      data: productIds,
      axisLabel:{
        interval:0,
        formatter:(productId: string)=>{
          const meta = MetaInfos.getProduct(productId)
          const name = meta?.name ?? productId
          if(meta?.iconUrl){
            return `{name|${name}} {${richKey(productId)}|}`
          }
          return `{name|${name}}`
        },
        rich: makeProductRichStyles(productIds)
      }
    },
    series: [
      {
        name: 'Net Amount',
        type: 'bar',
        data: data.map(p=>p.netPerMinute),
      },
    ],
  }
  return <EChartsReact option={option} style={{height:480}} />
}