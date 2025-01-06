import types
import sys
import json
import logging

registry = {}
objectRegistry = []


def register(obj):
    """
    注册函数或类
    Args:
        obj : 要注册的函数或类

    Raises:
        TypeError: 注册类型错误
    """
    if isinstance(obj, types.FunctionType):
        registry[obj.__name__] = obj
    elif isinstance(obj, type):
        objectRegistry.append(obj)
    else:
        raise TypeError("[dispatch]只支持注册函数或者类")
    return obj


def dispatch(topic, *args, **kwargs):
    results = []
    if topic in registry:
        obj = registry[topic]
        if not isinstance(obj, types.FunctionType):
            raise RuntimeError("[dispatch]注册的内容不是函数")
        else:
            results.append(obj(*args, **kwargs))
    else:
        for obj in objectRegistry:
            if not isinstance(obj, type):
                raise RuntimeError("[dispatch]注册的内容不是对象")
            elif hasattr(obj, topic):
                results.append(getattr(obj, topic)(obj, *args, **kwargs))
            else:
                pass

    if len(results) == 1:
        return results[0]
    return results


@register
def callables():
    return list(registry.keys())


@register
class reporter:
    def objects(self):
        results = {}
        for obj in objectRegistry:
            callables = [m for m in dir(obj) if callable(
                getattr(obj, m)) and not m.startswith('__')]
            results[obj.__qualname__] = callables
        return results


def process_request(request):
    id = request.get('Id', -1)
    topic = request.get('Topic', None)
    arg = request.get('Argument', None)

    if not topic:
        return {"Id": id, "Error": "未指定请求主题"}
    try:
        return {
            "Id": id,
            "Result": dispatch(topic, arg) if arg else dispatch(topic)
        }

    except Exception as e:
        return {"Id": id, "Error": f'{e}'}


class server:
    def __init__(self):
        pass

    def run(self):
        try:
            buffers = []
            braceCount = 0

            while True:
                line = sys.stdin.readline()
                if line == '':  # 检查是否为EOF
                    logging.info('读取结束，退出')
                    break
                content = line.strip()
                logging.info(f'读取到 {content}')

                buffers.append(content)
                # 更新大括号计数,如果为0,表示JSON字符串可能已经完整
                braceCount += content.count('{') - content.count('}')
                if braceCount == 0:
                    try:
                        j = json.loads(''.join(buffers))
                        buffers.clear()
                        braceCount = 0
                        logging.info('接收到json格式请求')
                        obj = process_request(j)
                        response = json.dumps(obj, separators=(
                            ',', ':'), ensure_ascii=False)
                        logging.info(f'反馈响应 {response}')
                        print(response)
                        sys.stdout.flush()
                    except json.JSONDecodeError:
                        pass
        except KeyboardInterrupt:
            logging.info('键盘中断,结束执行')
            pass


if __name__ == "__main__":
    print(dispatch("callables"))
    print(dispatch("objects"))

    server().run()
