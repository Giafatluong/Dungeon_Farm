const fs = require('fs');
const path = require('path');

function getAllMetaFiles(dir) {
  let results = [];
  const list = fs.readdirSync(dir);
  for (const file of list) {
    const fullPath = path.join(dir, file);
    const stat = fs.statSync(fullPath);
    if (stat && stat.isDirectory()) {
      results = results.concat(getAllMetaFiles(fullPath));
    } else if (file.endsWith('.meta')) {
      results.push(fullPath);
    }
  }
  return results;
}

const allMetas = [
  ...getAllMetaFiles('Assets'),
  ...getAllMetaFiles('Packages'),
  ...getAllMetaFiles('Library/PackageCache')
];

const guidToPath = new Map();
for (const mf of allMetas) {
  const content = fs.readFileSync(mf, 'utf8');
  const match = content.match(/guid:\s*([0-9a-f]{32})/);
  if (match) {
    guidToPath.set(match[1], mf);
  }
}
console.log(`Found ${guidToPath.size} meta GUIDs.`);

function checkScene(sceneFile) {
  console.log(`\nChecking ${sceneFile}...`);
  const content = fs.readFileSync(sceneFile, 'utf8');
  const scriptRegex = /--- !u!114 &(\d+)[\s\S]*?m_GameObject: \{fileID: (\d+)\}[\s\S]*?m_Script: \{fileID: \d+, guid: ([0-9a-f]{32}), type: 3\}[\s\S]*?m_Name: (.*)/g;
  let match;
  let missingCount = 0;
  const missingMap = new Map();
  while ((match = scriptRegex.exec(content)) !== null) {
    const fileId = match[1];
    const goId = match[2];
    const guid = match[3];
    if (!guidToPath.has(guid)) {
      missingCount++;
      if (!missingMap.has(guid)) {
        missingMap.set(guid, []);
      }
      missingMap.get(guid).push({ fileId, goId });
    }
  }
  if (missingCount === 0) {
    console.log(`All script GUIDs in ${sceneFile} are VALID!`);
  } else {
    console.log(`FOUND ${missingCount} instances of MISSING SCRIPTS in ${sceneFile}:`);
    for (const [guid, list] of missingMap) {
      console.log(`  Missing GUID: ${guid} (${list.length} instances, e.g. fileID: ${list[0].fileId}, goID: ${list[0].goId})`);
    }
  }
}

checkScene('Assets/_Scenes/Base.unity');
checkScene('Assets/_Scenes/Dungeon.unity');
