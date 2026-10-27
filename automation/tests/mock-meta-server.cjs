// Disposable local Graph API substitute. It cannot send a WhatsApp message.
const http=require('node:http');
const token=process.env.MOCK_META_TOKEN;
http.createServer((request,response)=>{
  if(request.method!=='POST'||request.url!=='/v26.0/123456789/messages'){
    response.writeHead(404);return response.end();
  }
  if(request.headers.authorization!=='Bearer '+token){
    response.writeHead(401,{'Content-Type':'application/json'});
    return response.end(JSON.stringify({error:{code:190}}));
  }
  let raw='';
  request.on('data',chunk=>{raw+=chunk;if(raw.length>10000)request.destroy();});
  request.on('end',()=>{
    let body;try{body=JSON.parse(raw);}catch{response.writeHead(400);return response.end();}
    if(body.messaging_product!=='whatsapp'||body.to!=='233201234567'||body.type!=='text'||
       typeof body.text?.body!=='string'){
      response.writeHead(400,{'Content-Type':'application/json'});
      return response.end(JSON.stringify({error:{code:100}}));
    }
    if(body.text.body.includes('GHS 45.00')){
      response.writeHead(429,{'Content-Type':'application/json'});
      return response.end(JSON.stringify({error:{code:4}}));
    }
    if(body.text.body.includes('Nothing has been placed')){
      response.writeHead(401,{'Content-Type':'application/json'});
      return response.end(JSON.stringify({error:{code:190}}));
    }
    response.writeHead(200,{'Content-Type':'application/json'});
    response.end(JSON.stringify({messages:[{id:'wamid.mock-outbound'}]}));
  });
}).listen(3001,'0.0.0.0');
