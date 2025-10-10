#!/bin/bash

# 快速运行单个或一组测试脚本（Linux/Mac）

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 显示用法
show_usage() {
    cat << EOF
用法: $0 [选项]

选项:
    -n, --name <名称>        运行包含指定名称的测试（模糊匹配）
    -c, --category <类别>    运行指定类别的测试（Unit/Integration/Performance）
    -m, --module <模块>      运行指定模块的测试（Physics/Network/Skill/Entity）
    -l, --list               列出所有测试，不运行
    -v, --verbose            显示详细输出
    -h, --help               显示此帮助信息

示例:
    $0 -n GetSkillInfo       运行所有包含 GetSkillInfo 的测试
    $0 -c Unit               运行所有单元测试
    $0 -m Physics            运行物理模块的所有测试
    $0 -l                    列出所有可用的测试
    $0 -n TypeConverter -v   运行 TypeConverter 测试（详细输出）
EOF
}

# 解析参数
TEST_NAME=""
CATEGORY=""
MODULE=""
LIST=false
VERBOSE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--name)
            TEST_NAME="$2"
            shift 2
            ;;
        -c|--category)
            CATEGORY="$2"
            shift 2
            ;;
        -m|--module)
            MODULE="$2"
            shift 2
            ;;
        -l|--list)
            LIST=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo -e "${RED}错误: 未知参数 '$1'${NC}"
            show_usage
            exit 1
            ;;
    esac
done

# 构建过滤器
FILTER=""

if [ -n "$TEST_NAME" ]; then
    FILTER="FullyQualifiedName~$TEST_NAME"
elif [ -n "$CATEGORY" ]; then
    FILTER="Category=$CATEGORY"
elif [ -n "$MODULE" ]; then
    FILTER="Module=$MODULE"
fi

# 构建测试命令
TEST_CMD="dotnet test"

if [ "$LIST" = true ]; then
    TEST_CMD="$TEST_CMD --list-tests"
else
    if [ -n "$FILTER" ]; then
        TEST_CMD="$TEST_CMD --filter \"$FILTER\""
    fi
    
    if [ "$VERBOSE" = true ]; then
        TEST_CMD="$TEST_CMD --logger \"console;verbosity=detailed\""
    fi
fi

# 显示运行信息
echo -e "${CYAN}============================================================${NC}"
if [ "$LIST" = true ]; then
    echo -e "${GREEN}列出所有测试${NC}"
else
    if [ -n "$FILTER" ]; then
        echo -e "${GREEN}运行测试: $FILTER${NC}"
    else
        echo -e "${GREEN}运行所有测试${NC}"
    fi
fi
echo -e "${CYAN}============================================================${NC}"
echo -e "${YELLOW}> $TEST_CMD${NC}"
echo ""

# 运行测试
eval $TEST_CMD
EXIT_CODE=$?

# 显示结果
echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ 测试通过${NC}"
else
    echo -e "${RED}❌ 测试失败 (退出码: $EXIT_CODE)${NC}"
fi

exit $EXIT_CODE

