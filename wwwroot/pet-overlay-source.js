import * as THREE from './vendor/three.module.min.js';
const pet2d=document.querySelector('#pet2d');
let mode='2d',targetX=120,targetY=120,currentX=120,currentY=120;

const renderer=new THREE.WebGLRenderer({canvas:document.querySelector('#three'),alpha:true,antialias:true,premultipliedAlpha:true});
renderer.setClearColor(0x000000,0);renderer.setPixelRatio(Math.min(devicePixelRatio,1.25));
const scene=new THREE.Scene();
let camera;
const robot=new THREE.Group();scene.add(robot);
const dark=new THREE.MeshStandardMaterial({color:0x082333,metalness:.75,roughness:.22});
const cyan=new THREE.MeshStandardMaterial({color:0x34eaff,emissive:0x00a9cc,emissiveIntensity:1.5,metalness:.45,roughness:.2});
const ice=new THREE.MeshStandardMaterial({color:0xd9ffff,emissive:0x36eaff,emissiveIntensity:1.2});
const body=new THREE.Mesh(new THREE.SphereGeometry(.82,24,18),dark);body.scale.set(1.18,.82,.82);robot.add(body);
const head=new THREE.Mesh(new THREE.BoxGeometry(1.45,.92,.78),dark);head.position.set(0,.72,.08);head.geometry.translate(0,0,0);robot.add(head);
const earGeo=new THREE.ConeGeometry(.25,.55,4);[-.52,.52].forEach((x,i)=>{const ear=new THREE.Mesh(earGeo,cyan);ear.position.set(x,1.42,0);ear.rotation.z=i?-.22:.22;robot.add(ear)});
[-.38,.38].forEach(x=>{const eye=new THREE.Mesh(new THREE.SphereGeometry(.105,16,12),ice);eye.position.set(x,.79,.43);robot.add(eye)});
const chest=new THREE.Mesh(new THREE.OctahedronGeometry(.22,0),cyan);chest.position.set(0,-.05,.72);robot.add(chest);
const tailPivot=new THREE.Group();tailPivot.position.set(.95,.05,0);robot.add(tailPivot);
const tail=new THREE.Mesh(new THREE.CylinderGeometry(.07,.11,1.05,10),cyan);tail.rotation.z=-1.05;tail.position.set(.42,.22,0);tailPivot.add(tail);
const ring=new THREE.Mesh(new THREE.TorusGeometry(1.3,.025,8,48),cyan);ring.rotation.x=Math.PI/2;ring.position.y=-.72;robot.add(ring);
scene.add(new THREE.HemisphereLight(0xa8faff,0x08101c,2.5));const key=new THREE.DirectionalLight(0x7ef6ff,5);key.position.set(3,5,5);scene.add(key);
const rim=new THREE.PointLight(0x168cff,9,12);rim.position.set(-3,1,4);scene.add(rim);

function resize(){
 const w=innerWidth,h=innerHeight;renderer.setSize(w,h,false);
 camera=new THREE.OrthographicCamera(-w/2,w/2,h/2,-h/2,-1000,1000);camera.position.z=500;camera.lookAt(0,0,0);
}
resize();addEventListener('resize',resize);

chrome.webview.addEventListener('message',({data})=>{
 if(data.type==='mode'){mode=data.mode;document.body.classList.toggle('mode3d',mode==='3d');return}
 if(data.type!=='cursor')return;
 const size=mode==='3d'?125:82,pad=20;
 const left=data.x>innerWidth-size-80,up=data.y>innerHeight-size-70;
 targetX=data.x+(left?-size-36:38);targetY=data.y+(up?-size-20:28);
 targetX=Math.max(pad,Math.min(innerWidth-size-pad,targetX));targetY=Math.max(pad,Math.min(innerHeight-size-pad,targetY));
});
let last=0;
function animate(t){
 requestAnimationFrame(animate);
 currentX+=(targetX-currentX)*.12;currentY+=(targetY-currentY)*.12;
 pet2d.style.transform='translate3d('+currentX+'px,'+currentY+'px,0)';
 if(mode!=='3d'||t-last<32)return;last=t;
 robot.position.set(currentX-innerWidth/2+62,innerHeight/2-currentY-62,0);
 robot.scale.setScalar(38);
 robot.rotation.y=Math.sin(t*.0012)*.32;robot.rotation.z=Math.sin(t*.0017)*.035;
 robot.position.y+=Math.sin(t*.003)*5;head.rotation.y=Math.sin(t*.0015)*.16;
 tailPivot.rotation.z=Math.sin(t*.006)*.38;ring.rotation.z=t*.0008;
 renderer.render(scene,camera);
}
requestAnimationFrame(animate);
