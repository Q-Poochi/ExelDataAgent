import os
import signal
import threading
import builtins
import pandas as pd
import matplotlib
matplotlib.use('Agg')  # Non-interactive backend
import matplotlib.pyplot as plt
import seaborn as sns
import io
import base64

# Allowed builtins, remove dangerous ones
ALLOWED_BUILTINS = {k: v for k, v in builtins.__dict__.items() if k not in (
    '__import__', 'eval', 'exec', 'open', 'file', 'compile'
)}

def safe_import(name, globals=None, locals=None, fromlist=(), level=0):
    allowed_modules = ('math', 'datetime', 'pandas', 'numpy', 'matplotlib', 'matplotlib.pyplot', 'seaborn')
    if name in allowed_modules:
        return builtins.__import__(name, globals, locals, fromlist, level)
    raise ImportError(f"Importing module '{name}' is strictly prohibited.")

ALLOWED_BUILTINS['__import__'] = safe_import

class TimeoutException(Exception):
    pass

def timeout_handler(signum, frame):
    raise TimeoutException("Execution timed out (30s limit).")

def execute_code(code: str, df: pd.DataFrame) -> list:
    """
    Executes Python code in a restricted environment.
    Returns a list of dictionaries with base64 encoded png images.
    """
    charts = []
    
    # Close any existing plots
    plt.close('all')

    # Create restricted environment
    restricted_globals = {
        '__builtins__': ALLOWED_BUILTINS,
        'pd': pd,
        'plt': plt,
        'sns': sns,
        'df': df
    }

    # Signal is only reliable on main thread in Unix. 
    # For Windows compatibility or threaded envs, we use threading.Timer
    timer = None
    if os.name == 'nt': # Windows timeout implementation
        def interrupt_main():
            import _thread
            _thread.interrupt_main()
        timer = threading.Timer(30.0, interrupt_main)
        timer.start()
    else: # Unix timeout implementation
        signal.signal(signal.SIGALRM, timeout_handler)
        signal.alarm(30)

    try:
        # EXECUTE UNTRUSTED CODE
        exec(code, restricted_globals, {})

        # Capture all generated figures
        fignums = plt.get_fignums()
        for i, fignum in enumerate(fignums):
            fig = plt.figure(fignum)
            buf = io.BytesIO()
            fig.savefig(buf, format='png', bbox_inches='tight', dpi=150)
            buf.seek(0)
            img_base64 = base64.b64encode(buf.read()).decode('utf-8')
            charts.append({
                "name": f"chart_{i+1}",
                "base64": img_base64,
                "mime_type": "image/png"
            })
            plt.close(fig)

    except KeyboardInterrupt: # Thrown by interrupt_main on Windows
         raise TimeoutException("Execution timed out (30s limit).")
    except Exception as e:
        raise Exception(f"Error during code execution: {str(e)}")
    finally:
        if os.name == 'nt' and timer:
            timer.cancel()
        elif os.name != 'nt':
            signal.alarm(0)

    return charts
