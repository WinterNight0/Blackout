# Blackout Scheduler

**Blackout Scheduler** is a simple yet powerful CLI tool for Windows that allows you to schedule **shutdown** or **hibernate** tasks, manage preset times, and customize task behavior. It is designed for end-users who want an easy way to automate PC power management.

---

## Features

- Schedule **shutdown** or **hibernate** at a specific time.
- Use **presets** to quickly apply saved schedules.
- Edit, delete, and list saved presets.
- Enable or disable scheduled tasks without removing them.
- Immediate shutdown or hibernate with a single command.
- User-friendly CLI interface with aliases for common commands.

---

## Commands

| Command                 | Alias       | Description                                              |                                            |
| ----------------------- | ----------- | -------------------------------------------------------- | ------------------------------------------ |
| `status`                | -           | Show current scheduled shutdown/hibernate and countdown. |                                            |
| `now`                   | -           | Execute shutdown or hibernate immediately.               |                                            |
| `set HH:mm`             | -           | Schedule a shutdown/hibernate at the specified time.     |                                            |
| `mode shutdown`         | `hibernate` | -                                                        | Switch between shutdown or hibernate mode. |
| `preset NAME`           | `ps`        | Apply a saved preset.                                    |                                            |
| `addpreset NAME HH:mm`  | `addps`     | Create a new preset.                                     |                                            |
| `editpreset NAME HH:mm` | `editps`    | Edit an existing preset.                                 |                                            |
| `delpreset NAME`        | `delps`     | Delete a preset.                                         |                                            |
| `listpresets`           | `lsps`      | Show all saved presets.                                  |                                            |
| `clear`                 | `clr`       | Clear the scheduled shutdown task.                       |                                            |
| `enable`                | -           | Enable the shutdown/hibernate task.                      |                                            |
| `disable`               | -           | Disable the shutdown/hibernate task.                     |                                            |

---

## Usage Examples

- Schedule a shutdown at 23:30:
  ```cmd
  blackout set 23:30
  ```
- Create a preset named `Night_Shutdown` at 23:30:
  ```cmd
  blackout addpreset Night_Shutdown 23:30
  ```
- Apply a preset:
  ```cmd
  blackout preset Night_Shutdown
  ```
  or
  ```cmd
  blackout ps Night_Shutdown
  ```
- Switch to hibernate mode:
  ```cmd
  blackout mode hibernate
  ```
- Immediate execution:
  ```cmd
  blackout now
  ```
- List all presets:
  ```cmd
  blackout listpresets
  ```
  or
  ```cmd
  blackout lsps
  ```

---

## Installation

### Compiling the Program

1. Open the project in **Visual Studio 2022** or your preferred IDE.
2. Build the solution (`Release` recommended) to generate `blackout.exe`.

### Packaging with Inno Setup

1. Open **Inno Setup Compiler**.
2. Open `Blackout.iss` (your setup script).
3. Edit the scheduled task names if needed (`Blackout_Schedule` recommended).
4. Compile the installer. The output will be an executable installer (`BlackoutInstaller.exe`) that installs the program and creates a launcher.

---

## Notes

- **Hibernate Mode:** Ensure your system supports hibernate. If not enabled, `mode hibernate` will show an error.
- All presets are saved in a text file in the executable folder:
  ```
  blackout_presets.txt
  ```
- The scheduled shutdown/hibernate task is created as a **user-level task**; admin privileges are not required.

---

## License

[MIT License](LICENSE)\
You are free to use, modify, and distribute this program.

