// Disposable API stub for the n8n runtime test. It never calls Groq.
const http=require('node:http');
const token=process.env.MOCK_GROQ_TOKEN;
http.createServer((request,response)=>{
  if(request.method!=='POST'||request.url!=='/v1/chat/completions'){
    response.writeHead(404);return response.end();
  }
  if(request.headers.authorization!=='Bearer '+token){
    response.writeHead(401);return response.end();
  }
  let text='';
  request.on('data',chunk=>{
    text+=chunk;
    if(text.length>100000) request.destroy();
  });
  request.on('end',()=>{
    let body;
    try{body=JSON.parse(text);}catch{response.writeHead(400);return response.end();}
    if(body.model!=='openai/gpt-oss-20b'||body.response_format?.json_schema?.strict!==true||
       !body.messages?.[0]?.content?.includes('Mock menu:')){
      response.writeHead(400);return response.end();
    }
    const message=body.messages[1].content.toLowerCase();
    if(message.includes('rate-limit')){
      response.writeHead(429,{'Content-Type':'application/json'});
      return response.end(JSON.stringify({error:{message:'rate limited'}}));
    }
    if(message.includes('timeout')){
      return setTimeout(()=>{if(!response.destroyed) response.end();},15000);
    }
    const choice=message.includes('bad-json')?'not json':JSON.stringify(
      message.includes('unknown-item')?{intent:'order',items:[{sku:'invented',quantity:1}]}:
      message.includes('jollof')?{intent:'order',items:[{sku:'jollof-chicken',quantity:2},{sku:'coke',quantity:1}]}:
      message.includes('menu')?{intent:'menu',items:[]}:
      message.includes('hello')?{intent:'greeting',items:[]}:
      {intent:'fallback',items:[]}
    );
    response.writeHead(200,{'Content-Type':'application/json'});
    response.end(JSON.stringify({choices:[{message:{content:choice}}]}));
  });
}).listen(3000,'0.0.0.0');
