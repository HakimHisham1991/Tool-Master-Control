import { spawn } from "child_process";
import * as path from "path";

const log = console.log;

(async () => {
  const port = process.env.PORT || "5000";
  const cncToolingDir = path.resolve(process.cwd(), "CNCToolingDatabase");
  
  log(`Starting CNC Tooling Database ASP.NET Core application on port ${port}...`);
  
  const dotnetProcess = spawn("dotnet", ["run"], {
    cwd: cncToolingDir,
    env: { ...process.env, PORT: port },
    stdio: "inherit"
  });
  
  dotnetProcess.on("error", (err) => {
    console.error("Failed to start .NET application:", err);
    process.exit(1);
  });
  
  dotnetProcess.on("exit", (code) => {
    log(`.NET application exited with code ${code}`);
    process.exit(code || 0);
  });
  
  process.on("SIGINT", () => {
    log("Received SIGINT, stopping .NET application...");
    dotnetProcess.kill("SIGINT");
  });
  
  process.on("SIGTERM", () => {
    log("Received SIGTERM, stopping .NET application...");
    dotnetProcess.kill("SIGTERM");
  });
})();
