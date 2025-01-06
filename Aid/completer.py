import dispatch
import logging
import uuid


@dispatch.register
def Commands():
    return [
        {
            "Identity": "HelloWorld",
            "Label": "completer中的测试命令",
            "Group": "Test",
            "Description": "测试请求与反馈"
        }
    ]


@dispatch.register
def Completions():
    return {
        "ContentTypes": ["C/C++"],
        "Keywords": ["GUID", "IMPORT"],
        "LowerCaseKeywords": ["dbg"],
        "ResponseFirstSpace": True
    }


@dispatch.register
def CompletionSuggestedItems(argument):
    word: str = argument.get("Word", None)
    words: list[str] = argument.get("Words", [])
    position: int = argument.get('Position', 0)
    if word == 'GUID':
        return [
            {
                "DisplayText": "生成GUID",
                "InsertText": f'//{uuid.uuid4()}',
                "Description": "生成新的GUID,并插入到目标位置",
                "ImageMonikerId": 1385
            }
        ]
    if word.lower() == 'dbg':
        return [
            {
                "DisplayText": "生成调试警告",
                "InsertText": f'//{uuid.uuid4()}',
                "Description": "生成调试警告,并插入到目标位置",
                "ImageMonikerId": 3931
            }
        ]
    if word == 'IMPORT':
        return [
            {
                "DisplayText": "导入标准库",
                "Description": "导入标准库",
                "ImageMonikerId": 1385,
                "Commits": [
                    {
                        "StartPosition": 0,
                        "EndPosition": 0,
                        "Text": '#include <string> \r\n#include <vector>\r\n'
                    },
                    {
                        "StartPosition": position - len('IMPORT'),
                        "EndPosition": position,
                        "Text": 'std::string strVal{};'
                    }
                ]
            }
        ]
    if len(words) > 0:
        result = []
        for w in words:
            result.append({
                "DisplayText": w.upper(),
                "Description": "推荐变量命名",
                "ImageMonikerId": 3931,
            })
        return result
    raise RuntimeError("无法识别的信息")


@dispatch.register
def Topics():
    results = dispatch.callables()
    # TODO FIXME 以类方式注册的方法也需要报告
    return results


if __name__ == "__main__":
    logging.basicConfig(
        level=logging.DEBUG,
        filename="completer.log",
        filemode='w',
        format='%(name)s - %(levelname)s - %(message)s'
    )
    dispatch.server().run()
