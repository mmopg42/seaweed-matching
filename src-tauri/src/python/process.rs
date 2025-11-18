use serde::{Deserialize, Serialize};
use std::io::{BufRead, BufReader, Write};
use std::process::{Child, ChildStdin, ChildStdout, Command, Stdio};
use std::sync::Mutex;

#[derive(Debug, Serialize, Deserialize)]
struct JsonRpcRequest {
    method: String,
    params: serde_json::Value,
}

#[derive(Debug, Serialize, Deserialize)]
struct JsonRpcResponse {
    result: Option<serde_json::Value>,
    error: Option<String>,
}

pub struct PythonProcess {
    stdin: Mutex<ChildStdin>,
    stdout: Mutex<BufReader<ChildStdout>>,
    #[allow(dead_code)]
    child: Mutex<Child>,
}

impl PythonProcess {
    pub fn new(script_path: &str) -> Result<Self, String> {
        let mut child = Command::new("python")
            .arg(script_path)
            .stdin(Stdio::piped())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .spawn()
            .map_err(|e| format!("Failed to start Python process: {}", e))?;

        let stdin = child
            .stdin
            .take()
            .ok_or("Failed to capture Python stdin")?;
        let stdout = child
            .stdout
            .take()
            .ok_or("Failed to capture Python stdout")?;

        Ok(Self {
            stdin: Mutex::new(stdin),
            stdout: Mutex::new(BufReader::new(stdout)),
            child: Mutex::new(child),
        })
    }

    pub fn call(
        &self,
        method: &str,
        params: serde_json::Value,
    ) -> Result<serde_json::Value, String> {
        let request = JsonRpcRequest {
            method: method.to_string(),
            params,
        };

        let request_json = serde_json::to_string(&request)
            .map_err(|e| format!("Failed to serialize request: {}", e))?;

        // Send request
        let mut stdin = self
            .stdin
            .lock()
            .map_err(|e| format!("Failed to lock stdin: {}", e))?;
        writeln!(stdin, "{}", request_json)
            .map_err(|e| format!("Failed to write to Python stdin: {}", e))?;
        stdin
            .flush()
            .map_err(|e| format!("Failed to flush stdin: {}", e))?;

        // Read response
        let mut stdout = self
            .stdout
            .lock()
            .map_err(|e| format!("Failed to lock stdout: {}", e))?;
        let mut response_line = String::new();
        stdout
            .read_line(&mut response_line)
            .map_err(|e| format!("Failed to read from Python stdout: {}", e))?;

        let response: JsonRpcResponse = serde_json::from_str(&response_line)
            .map_err(|e| format!("Failed to parse Python response: {}", e))?;

        if let Some(error) = response.error {
            return Err(error);
        }

        Ok(response.result.unwrap_or(serde_json::Value::Null))
    }

    pub fn ping(&self) -> Result<String, String> {
        let result = self.call("ping", serde_json::Value::Null)?;
        result
            .as_str()
            .map(|s| s.to_string())
            .ok_or_else(|| "Invalid ping response".to_string())
    }
}

impl Drop for PythonProcess {
    fn drop(&mut self) {
        if let Ok(mut child) = self.child.lock() {
            let _ = child.kill();
        }
    }
}
