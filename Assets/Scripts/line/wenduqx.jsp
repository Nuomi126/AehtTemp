<%@ page contentType="text/html;charset=UTF-8" language="java" %>
<html>
<head>
    <meta charset="utf-8">
    <title>${companyname}【温度曲线】</title>
    <%@include file="common/head.jsp" %>
    <%@include file="common/tag.jsp" %>
    <%@include file="common/footer.jsp" %>
    <link rel="stylesheet" href="${pageContext.request.contextPath}/css/wenduqx.css">
    <script async="" src="${pageContext.request.contextPath}/js/char/analytics.js"></script>
    <script src="${pageContext.request.contextPath}/js/char/utils.js"></script>
</head>
<body>
<script type="application/javascript" src="${pageContext.request.contextPath}/js/cookie.js"></script>
<script type="application/javascript" src="${pageContext.request.contextPath}/js/wenduqx.js"></script>
<script src="https://s3.pstatp.com/cdn/expire-1-M/jquery/3.0.0/jquery.min.js"></script>
<div class="container-fluid" style="padding-left: 0;padding-right: 0;">
    <header class="rounded">
        <nav class="navbar navbar-light fixted-top navbar-expand-md text-center"
             style="background-color: rgb(0,200,180);padding-top: 0;padding-bottom: 0;">
            <a href="javascript:void(0);" class="navbar-brand" style="color: white;">
                <i class="fa fa-angle-left" aria-hidden="true"></i>
            </a>
            <span class="navbar-text mx-auto lead well" style="color: white;position: relative;left: -15px;">
        <strong>${companyname}【温度曲线】</strong>
        </span>
        </nav>
    </header>
</div>
<div class="clearfix" style="height: 5px;background-color: white;"></div>
<div class="row justify-content-around no-gutters head-row" style="background-color: white;max-width: 100%;">
    <div id="riq" class="col-4 btnWrapper border rounded rounded-top" style="max-width:26%;">
        <button id="riqselect" class="btn btn-default btn-sm collapseBtn btn-block" data-toggle="collapse" href="#collapseRiq"
                aria-expanded="false" aria-controls="collapseRiq" style="width:100%;">
            日期&nbsp;<span class="fa fa-caret-down" aria-hidden="true"></span>
        </button>
    </div>
    <div id="gongquAndChejian" class="col-4 btnWrapper border rounded rounded-top" style="max-width:25%;">
        <button id="gongquselect" class="btn btn-default btn-sm collapseBtn btn-block" data-toggle="collapse"
                href="#collapseGongquAndChejian"
                aria-expanded="false" aria-controls="collapseGongquAndChejian">
            工区&nbsp;<span class="fa fa-caret-down" aria-hidden="true"></span>
        </button>
    </div>

    <div id="caohao" class="col-4 btnWrapper border rounded rounded-top" style="max-width:15%;">
        <button id="caohaoselect" class="btn btn-default btn-sm collapseBtn btn-block" data-toggle="collapse" href="#collapseCaohao"
                aria-expanded="false" aria-controls="collapseCaohao" style="width:100%;">
            槽号&nbsp;<span class="fa fa-caret-down" aria-hidden="true"></span>
        </button>
    </div>

    <div id="cewendian" class="col-4 btnWrapper border rounded rounded-top" style="max-width:17%;">
        <button id="cewendianselect" class="btn btn-default btn-sm collapseBtn btn-block" data-toggle="collapse" href="#collapseCewendian"
                aria-expanded="false" aria-controls="collapseCewendian" style="width:100%;text-align:center;">
            测温点&nbsp;<span class="fa fa-caret-down" aria-hidden="true"></span>
        </button>
    </div>
    <div id="dynamiccurve" class="col-4 border rounded rounded-top" style="max-width:11%;">
        <input id="btndynamic" type="button"  value="历史" onclick = "dynamiccurveprocess()"
               style="width:100%;height:100%;background-color:rgb(0,200,180);text-align:center;padding:0px;"/>
    </div>
</div>

<div class="collapse" id="collapseGongquAndChejian">
    <div class="card card-body">
        <div class="row justify-content-around no-gutters" style="font-size: small;">
            <c:forEach var="gongqu" items="${gongqus}">
                <div class="col-5 gongquAndChejianDiv">
                    <span class="gongquAndChejianSelect">${gongqu.chejian.chejianm}${gongqu.gongqum}</span>
                    <span class="fa float-right gongquAndChejianSpan" aria-hidden="true" value="${gongqu.chejian.chejianh * 100 + gongqu.gongquh}"></span>
                </div>
            </c:forEach>
            <div class="form-group col-12" style="padding-top: 10px;">
                <div class="btn-group btn-group-sm" style="width: inherit;">
                    <button id="resetGongquAndChejian" type="reset" class="btn btn-default border reset"
                            style="width: 50%;color: black;">
                        <strong>重置</strong>
                    </button>
                    <button type="button" class="btn btn-primary border border-0" onclick = "readtechdataanddraw_scope()"
                            style="width: 50%;background-color:rgb(0,200,180);">
                        <strong>确认</strong>
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>
<div class="collapse" id="collapseRiq">
    <div class="card justify-content-around no-gutters card-body">
        <div class="row no-gutters" style="font-size: small;">
            <div class="col-12" style="text-align: center;">
                日期选择:<input id="date" type="date">
            </div>
            <div class="w-100" style="height: 10px;"></div>
            <div class="form-group col-12">
                <div class="btn-group btn-group-sm" style="width: inherit;">
                    <button id="clearRiq" type="reset" class="btn btn-default border reset"
                            style="width: 50%;color: black;">
                        <strong>重置</strong>
                    </button>
                    <button type="button" class="btn btn-primary border border-0" onclick = "readtechdataanddraw_date()"
                            style="width: 50%;background-color:rgb(0,200,180);">
                        <strong>确认</strong>
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>
<div class="collapse" id="collapseCaohao">
    <div class="card card-body">
        <div class="row justify-content-around no-gutters" style="font-size: small;">
            <c:if test="${caohaos != null}">
                <div id="caohaoGroup1" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 1}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter1" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup2" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 2}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter2" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup3" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 3}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter3" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup4" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 4}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter4" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup5" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 5}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter5" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup6" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 6}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter6" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup7" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 7}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter7" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup8" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 8}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter8" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup9" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 9}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter9" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup10" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 10}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter10" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup11" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 11}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter11" class="w-100" style="height: 10px;"></div>
                <div id="caohaoGroup12" class="card card-body">
                    <div class="row justify-content-around no-gutters" style="font-size: small;">
                        <c:forEach var="caohao" items="${caohaos}">
                            <c:if test="${caohao.gongquh == 12}">
                                <div class="col-3 caohaoDiv">
                                    <span class="caohaoSelect" value="${caohao.caoh}*">${caohao.caoh}</span>
                                    <span class="fa float-right caohaoSpan" aria-hidden="true" value="${caohao.caoh}"></span>
                                </div>
                            </c:if>
                        </c:forEach>
                    </div>
                </div>
                <div id="caohaoSplitter12" class="w-100" style="height: 10px;"></div>
            </c:if>

            <div class="form-group col-12" style="padding-top: 10px;">
                <div class="btn-group btn-group-sm" style="width: inherit;">
                    <button id="resetCaohao" type="reset" class="btn btn-default border reset"
                            style="width: 50%;color: black;">
                        <strong>重置</strong>
                    </button>
                    <button type="button" class="btn btn-primary border border-0" onclick = "readtechdataanddraw_caoh()"
                            style="width: 50%;background-color:rgb(0,200,180);">
                        <strong>确认</strong>
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

<style>
    #lv2U1,#lv2U2,#lv2U3,#lv2U4,#lv2U5,#lv2U6{
        display: none;
    }
</style>


<div class="collapse" id="collapseCewendian">
    <p id="warning" style="display: none; color: red;">最多只能选择6个测温点</p>
    <p>你选择的测温点为: <span id="selectedItems"></span></p>
    <div class="tree">
        <ul id="lv1U1">
            <img src="../img/add.png" id="lv1M1" style="display: block; float: left; width: 19px; height: 19px;"/>
            <li style="display:contents;width: 60px; height: 30px;">A面阴极
                <ul id="lv2U1" style="clear: left;">
                    <c:forEach var="cewendian" items="${amianyinjis}">
                        <c:if test="${cewendian.directionindex==1}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="A面阴极A${cewendian.nodeindex}-1,${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">A${cewendian.nodeindex}-1</li>
                        </c:if>
                        <c:if test="${cewendian.directionindex==2}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="A面阴极A${cewendian.nodeindex}-2,${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">A${cewendian.nodeindex}-2</li>
                        </c:if>
                    </c:forEach>
                </ul>
            </li>
        </ul>
        <ul id="lv1U2">
            <img src="../img/add.png" id="lv1M2" style="display: block; float: left;  width: 19px; height: 19px;"/>
            <li style="display:contents; width: 60px; height: 30px;">B面阴极
                <ul id="lv2U2" style="clear: left;">
                    <c:forEach var="cewendian" items="${bmianyinjis}">
                        <c:if test="${cewendian.directionindex==1}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="B面阴极B${cewendian.nodeindex}-1,${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">B${cewendian.nodeindex}-1</li>
                        </c:if>
                        <c:if test="${cewendian.directionindex==2}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="B面阴极B${cewendian.nodeindex}-2,${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">B${cewendian.nodeindex}-2</li>
                        </c:if>
                    </c:forEach>
                </ul>
            </li>
        </ul>
        <ul id="lv1U3">
            <img src="../img/add.png" id="lv1M3" style="display: block; float: left;  width: 19px; height: 19px;"/>
            <li style="display:contents; width: 60px; height: 30px;">A面钢窗
                <ul id="lv2U3" style="clear: left;">
                    <c:forEach var="cewendian" items="${amiangangchuangs}">
                            <input type="checkbox" id="secondCheck2" style="display: block; float: left; width: 19px; height: 19px;" value="A面钢窗WA${cewendian.nodeindex},${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">WA${cewendian.nodeindex}</li>
                    </c:forEach>
                </ul>
            </li>
        </ul>
        <ul id="lv1U4">
            <img src="../img/add.png" id="lv1M4" style="display: block; float: left;  width: 19px; height: 19px;"/>
            <li style="display:contents; width: 60px; height: 30px;">B面钢窗
                <ul id="lv2U4" style="clear: left;">
                    <c:forEach var="cewendian" items="${bmiangangchuangs}">
                        <input type="checkbox" id="secondCheck2" style="display: block; float: left; width: 19px; height: 19px;" value="B面钢窗WB${cewendian.nodeindex},${cewendian.sampleindex}"/>
                        <li style="width: 120px; height: 30px;">WB${cewendian.nodeindex}</li>
                    </c:forEach>
                </ul>
            </li>
        </ul>
        <ul id="lv1U5">
            <img src="../img/add.png" id="lv1M5" style="display: block; float: left;  width: 19px; height: 19px;"/>
            <li style="display:contents; width: 60px; height: 30px;">端部
                <ul id="lv2U5" style="clear: left;">
                    <c:forEach var="cewendian" items="${duanbus}">
                        <c:if test="${cewendian.posindex==2}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="出铝端T${cewendian.nodeindex},${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">出铝端T${cewendian.nodeindex}</li>
                        </c:if>
                        <c:if test="${cewendian.posindex==3}">
                            <input type="checkbox" id="secondCheck1" style="display: block; float: left; width: 19px; height: 19px;" value="烟道端G${cewendian.nodeindex},${cewendian.sampleindex}"/>
                            <li style="width: 120px; height: 30px;">烟道端G${cewendian.nodeindex}</li>
                        </c:if>
                    </c:forEach>
                </ul>
            </li>
        </ul>
        <ul id="lv1U6">
            <img src="../img/add.png" id="lv1M6" style="display: block; float: left;  width: 19px; height: 19px;"/>
            <li style="display:contents; width: 60px; height: 30px;">底部
                <ul id="lv2U6" style="clear: left;">
                    <c:forEach var="cewendian" items="${dibus}">
                        <input type="checkbox" id="secondCheck2" style="display: block; float: left; width: 19px; height: 19px;" value="底部${cewendian.nodeindex},${cewendian.sampleindex}"/>
                        <li style="width: 120px; height: 30px;">
                                <script>
                                    // 假设您有一个变量 item6.nodeindex 来存储这个数
                                    var nodeIndex = ${cewendian.nodeindex};
                                    // 判断是否为偶数
                                    if (nodeIndex % 2 === 0) {
                                        document.write("FB" + nodeIndex);
                                    } else {
                                        document.write("FA" + nodeIndex);
                                    }
                                </script>
                        </li>
                    </c:forEach>
                </ul>
            </li>
        </ul>
    </div>

    <div class="form-group col-12" style="padding-top: 10px;">
        <div class="btn-group btn-group-sm" style="width: inherit;">
            <button id="resetCewendian" type="reset" class="btn btn-default border" onclick = "refresTemcurve()"
                    style="width: 50%;color: black;">
                <strong></strong>
            </button>
            <button type="button" class="btn btn-primary border border-0" onclick = "readtechdataanddraw_cewendian()"
                    style="width: 50%;background-color:rgb(0,200,180);">
                <strong>确认</strong>
            </button>
        </div>
    </div>
</div>

<form id="conditionForm" onsubmit = "return false" action="##" method="post">
    <input id="gongqhHidden" name="gongqh" type="hidden">
    <input id="chejhHidden" name="chejh" type="hidden">
    <input id="riqHidden" name="riq" type="hidden">
    <input id="companyindexHidden" name="companyindex" type="hidden" value="${companyindex}">
    <input id="guiorientflagHidden" name="guiorientflag" type="hidden" value = "1">
    <input id="curveDrawLockHidden" name="curveDrawLock" type="hidden" value = "1">
    <input id="caohHidden" name="caoh" type="hidden">
    <input id="companyidHidden" name="companyid" type="hidden" value = "${companyid}">
</form>


<script type="application/javascript" src="${pageContext.request.contextPath}/js/histemcurve.js"></script>
<script type="application/javascript" src="${pageContext.request.contextPath}/js/hisdynamictemcurve.js"></script>
<%--<script type="application/javascript" src="${pageContext.request.contextPath}/js/1.js"></script>--%>
<div class="container-fluid canvas-wrapper">
    <canvas id="canvas"
            style="width: auto;position: relative;left: -4px;;margin-top: -50px">
    </canvas>
</div>

<script type="text/javascript">
    $(function() {
        $("#lv1M1").click(function() {
            if($("#lv2U1").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M1").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M1").attr("src", "../img/cut.png");
            }
            $("#lv2U1").slideToggle(300);
        });
        $("#lv1M2").click(function() {
            if($("#lv2U2").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M2").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M2").attr("src", "../img/cut.png");
            }
            $("#lv2U2").slideToggle(300);
        });
        $("#lv1M3").click(function() {
            if($("#lv2U3").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M3").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M3").attr("src", "../img/cut.png");
            }
            $("#lv2U3").slideToggle(300);
        });
        $("#lv1M4").click(function() {
            if($("#lv2U4").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M4").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M4").attr("src", "../img/cut.png");
            }
            $("#lv2U4").slideToggle(300);
        });
        $("#lv1M5").click(function() {
            if($("#lv2U5").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M5").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M5").attr("src", "../img/cut.png");
            }
            $("#lv2U5").slideToggle(300);
        });
        $("#lv1M6").click(function() {
            if($("#lv2U6").is(":visible")) {
                //                      alert("隐藏内容");
                $("#lv1M6").attr("src", "../img/add.png");
            } else {
                //                      alert("显示内容");
                $("#lv1M6").attr("src", "../img/cut.png");
            }
            $("#lv2U6").slideToggle(300);
        });

        $("#allCheck").click(function(){
            $("input[type=checkbox]").prop("checked",$("#allCheck").prop("checked"));
        });

        $("#secondCheck1").click(function(){
            $("input[name=checkbox]").prop("checked",$("#secondCheck1").prop("checked"));
        });

        $("#secondCheck2").click(function(){
            $("input[name=checkbox]").prop("checked",$("#secondCheck2").prop("checked"));
        });

    });

    var flag=0;
    var histemperature=[];
    var dynamictemperature=[];
    var timer;
    var activedate = 0;
    var activepotname = 0;

    var buttonCaohao = document.getElementById('caohaoselect');
    var buttonCewendian = document.getElementById('cewendianselect');

    // 添加点击事件监听器到第一个按钮
    buttonCaohao.addEventListener('click', function() {
        // 当按钮被点击时执行以下代码
        flag = 0; // 将 flag 设置为 0
        console.log('Flag value is now:', flag);
    });

    // 添加点击事件监听器到第二个按钮
    buttonCewendian.addEventListener('click', function() {
        // 当按钮被点击时执行以下代码
        flag = 0; // 将 flag 设置为 0
        console.log('Flag value is now:', flag);
    });


    const checkboxes = document.querySelectorAll('input[type="checkbox"]');
    const maxSelections = 6;
    let selectedCount = 0;

    checkboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            if (this.checked) {
                selectedCount++;
                if (selectedCount > maxSelections) {
                    this.checked = false;
                    selectedCount--;
                    // alert("最多只能选择6个测温点");
                    document.getElementById('warning').style.display = 'block';
                }
            } else {
                selectedCount--;
                document.getElementById('warning').style.display = 'none';
            }
            updateSelectedItems();
        });
    });

    function updateSelectedItems() {
        const selectedItems = [];
        const selectedIndex = [];
        checkboxes.forEach(checkbox => {
            if (checkbox.checked) {
                var value=checkbox.value;
                selectedItems.push(value.substring(0, value.indexOf(",")));
                selectedIndex.push(value.substring(value.indexOf(",") + 1));
                // selectedIndex.push(value);
            }
        });
        document.getElementById('selectedItems').textContent = selectedItems.join(', ');
        // console.log(selectedIndex);
        return selectedIndex;
    }

    function dynamiccurveprocess()
    {
        hidecollapsebtn();
        var btnDynamic_tmp = document.getElementById("btndynamic");
        var tmpPos = document.getElementById("riqselect").innerHTML.indexOf("日期");
        var tmpPos8 = document.getElementById("caohaoselect").innerHTML.indexOf("槽号");
        if(btnDynamic_tmp.value=== "动态") {
            btnDynamic_tmp.value = "历史";
            var se =updateSelectedItems();
            if(se.length!==0) {

                if (tmpPos >= 0) {
                    alert("请选择日期！");
                } else {
                    if (tmpPos8 >= 0) {
                        alert("请选择槽号！");
                    } else {
                        console.log(histemperature.length);
                        console.log(histemperature);
                        if (histemperature.length !== 0) { // 如果已经有了数据，直接绘制图表
                            if(histemperature[0].temList.length!==0){ //判断有数据表，但温度数据是否为空
                                cewendiandraw();
                            }else { // 否则进行异步请求
                                readtechdata_asyn().then(function () {
                                    cewendiandraw(); // 在 Promise 解决后执行 cewendiandraw 函数
                                }).catch(function (error) {
                                    console.error("An error occurred:", error);
                                });
                            }

                        }else { // 否则进行异步请求
                            readtechdata_asyn().then(function () {
                                cewendiandraw(); // 在 Promise 解决后执行 cewendiandraw 函数
                            }).catch(function (error) {
                                console.error("An error occurred:", error);
                            });
                        }
                    }
                }
            }else {
                alert("请选择测温点！")
            }
        }
        else {
            btnDynamic_tmp.value = "动态";
            var se =updateSelectedItems();
            if(se.length!==0){
                if (tmpPos8 >= 0) {
                    alert("请选择槽号！");
                } else {
                    drawCurve();
                }

            }else {
                alert("请选择测温点！")
            }

        }
    }

    function hidecollapsebtn()
    {
        $("#collapseGongquAndChejian").removeClass('show');
        $("#collapseRiq").removeClass('show');
        $("#collapseCaohao").removeClass('show');
        $("#collapseCewendian").removeClass('show');
    }

    function readtechdataanddraw_scope() {
        $("#collapseGongquAndChejian").removeClass('show');
        var tmpGongqu = $("span.fa-check.gongquAndChejianSpan").attr('value');
        if (tmpGongqu != null) {
            var tmpText = "";
            var gongqhAndChejh = Number($("span.fa-check.gongquAndChejianSpan").attr('value'));
            tmpText = getgongquname(gongqhAndChejh);
            document.getElementById("gongquselect").innerHTML = tmpText.trim() + "&nbsp;<span class='fa fa-caret-down' aria-hidden='true'></span>";
            document.getElementById("gongqhHidden").value = gongqhAndChejh % 100;

            //-----------------------//
            document.getElementById("caohaoselect").innerHTML = "槽号&nbsp;<span class='fa fa-caret-down' aria-hidden='true'></span>";
            $(".caohaoSelect").each(function () {
                $(this).css('color', 'black');
            });
            if($("span.fa-check.caohaoSpan") != null) {
                $("span.fa-check.caohaoSpan").removeClass("fa-check");
            }
            var can1 = document.getElementById("canvas");
            var ctx = can1.getContext("2d");
            ctx.clearRect(0, 0, can1.width, can1.height);
            flag=0;
            alert("请选择槽号！");
        }
    }

    var tmpGongquh = [];
    var tmpGongqum = [];
    var gongquindex = 0;
    var tmpTextC = "";
    var tmpTextQ = "";
    <c:if test="${gongqus != null}">
        <c:forEach var="gongqu" items="${gongqus}">
            tmpGongquh[gongquindex] = ${gongqu.chejian.chejianh * 100 + gongqu.gongquh};
            tmpTextC = "${gongqu.chejian.chejianm}";
            tmpTextQ =  "${gongqu.gongqum}";
            tmpGongqum[gongquindex] = tmpTextC + tmpTextQ;
            gongquindex = gongquindex + 1;
        </c:forEach>
    </c:if>

    function getgongquname(gongquchejh)
    {
        if(tmpGongquh.length > 0) {
            for (var j = 0; j < tmpGongquh.length; j++) {
                if(tmpGongquh[j] == gongquchejh) {
                    return tmpGongqum[j];
                }
            }
        }
        else {
            return "";
        }
    }

    function readtechdataanddraw_date()
    {
        $("#collapseRiq").removeClass('show');
        document.getElementById("riqselect").innerHTML = $("#date").val().toString() + "&nbsp;<span class='fa fa-caret-down' aria-hidden='true'></span>";
        var tmpPos = document.getElementById("caohaoselect").innerHTML.indexOf("槽号");
        var se=updateSelectedItems();
        if(tmpPos >= 0) {
            alert("请选择槽号！");
        }
        else{
            var btnDynamic_tmp = document.getElementById("btndynamic");
            if (btnDynamic_tmp.value === "历史") {
                if(se.length===0){
                    alert("请选择测温点！");
                }

                histemperature=[];
                readtechdata_asyn().then(function() {
                    cewendiandraw(); // 在 Promise 解决后执行 cewendiandraw 函数
                }).catch(function(error) {
                    console.error("An error occurred:", error);
                });

            } else {
                if(se.length===0){
                    alert("请选择测温点！");
                }else {
                    // flag=0;
                    // drawCurve();
                }
            }

        }

    }

    function readtechdataanddraw_caoh()
    {
        $("#collapseCaohao").removeClass('show');
        var tmpPos = document.getElementById("riqselect").innerHTML.indexOf("日期");
        var tmpPos8 = document.getElementById("gongquselect").innerHTML.indexOf("工区");
        var se = updateSelectedItems();
        var btnDynamic_tmp = document.getElementById("btndynamic");
        if(tmpPos >= 0) {
            alert("请选择日期！");
        }
        else {
            if(tmpPos8 === 0)//工区名中间有“工区”两个字
            {
                alert("请选择工区！");
            }
            else {
                let tmpPotName = $("span.fa-check.caohaoSpan").attr('value');
                if(tmpPotName != null) {
                    document.getElementById("caohaoselect").innerHTML = tmpPotName.toString() + "&nbsp;<span class='fa fa-caret-down' aria-hidden='true'></span>";
                }

                if (btnDynamic_tmp.value === "历史") {
                    if(se.length===0){
                        alert("请选择测温点！");
                    }

                    histemperature=[];
                    readtechdata_asyn().then(function() {
                        cewendiandraw(); // 在 Promise 解决后执行 cewendiandraw 函数
                    }).catch(function(error) {
                        console.error("An error occurred:", error);
                    });

                }else {
                    if(se.length===0){
                        alert("请选择测温点！");
                    }else {
                        // flag=0;
                        drawCurve();
                        // setTimeout(drawCurve,2000);
                    }
                }
            }
        }
    }

    function alarm(){
        var can1 = document.getElementById("canvas");
        var ctx = can1.getContext("2d");

        ctx.clearRect(0, 0, can1.width, can1.height);
        console.log("qingchu");

        can1.width = 300; // 设置画布宽度为 500 像素
        can1.height = 200; // 设置画布高度为 300 像素

        ctx.font = "15px Arial";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle"; // 设置基线为中间

        var text = "曲线数据正在加载中...";

        var textX = can1.width / 2;
        var textY = can1.height / 2;

        ctx.fillText(text, textX, textY); // 在计算出的位置绘制文本

    }

    function readtechdataanddraw_cewendian(){
        $("#collapseCewendian").removeClass('show');
        var tmpPos = document.getElementById("riqselect").innerHTML.indexOf("日期");
        var tmpPos8 = document.getElementById("caohaoselect").innerHTML.indexOf("槽号");
        if(tmpPos >= 0) {
            alert("请选择日期！");
        }
        else {
            if (tmpPos8 >= 0) {
                alert("请选择槽号！");
            } else {
                var btnDynamic_tmp = document.getElementById("btndynamic");
                if (btnDynamic_tmp.value === "历史") {
                    if(histemperature.length!==0)
                    { // 如果已经有了数据，直接绘制图表
                        cewendiandraw();
                    } else { // 否则进行异步请求
                        readtechdata_asyn().then(function() {
                            cewendiandraw(); // 在 Promise 解决后执行 cewendiandraw 函数
                        }).catch(function(error) {
                            console.error("An error occurred:", error);
                        });
                    }
                }else{
                    // flag=0;
                    drawCurve();
                    // setTimeout(drawCurve,2000);
                }
            }
        }
    }

    function cewendiandraw() {
        var k=0;
        selectednames=new Array();
        selectedindex=new Array();
        checkboxes.forEach(checkbox => {
            if (checkbox.checked) {
                var value=checkbox.value;
                selectednames[k]=value.substring(0, value.indexOf(","));
                selectedindex[k]=value.substring(value.indexOf(",") + 1);
                k++;
            }
        });

        if (histemperature.length!==0) {
            recordindex_day = new Array();
            datas_day = new Array();
            temps = new Array();
            for (var a = 0; a < selectedindex.length; a++) {
                var j = 0;
                temps[a] = [];
                console.log("a"+selectedindex[a]);
                for (var i = 0; i < 720; i++) {
                    recordindex_day[i] = i + 1;
                    if (j < histemperature[selectedindex[a]].temList.length && histemperature[selectedindex[a]].temList[j].recordIndex === (i + 1)) {
                        temps[a][i] = histemperature[selectedindex[a]].temList[j].wendu;
                        j++;
                    } else {
                        temps[a][i] = 0;
                    }
                }
            }

            if (recordindex_day.length > 0) {
                var tempTime;
                var datatemp;
                for (jk = 0; jk < 7; jk++) {
                    datatemp = (jk * 240) / 60;
                    tempTime = checkNum_2(datatemp) + ":";
                    tempTime = tempTime + "00";
                    datas_day[jk] = tempTime;
                }
            }

            drawhistemcurve(selectednames,temps,datas_day,recordindex_day);

        }else
        {
            var can1 = document.getElementById("canvas");
            var ctx = can1.getContext("2d");
            ctx.clearRect(0, 0, can1.width, can1.height);

            alert("没有您查询的温度历史曲线！")
        }
    }

    function readtechdata_asyn() {
        return new Promise(function(resolve, reject) {
            var gongqhAndChejh = $("span.fa-check.gongquAndChejianSpan").attr('value');
            var date = $("#date").val();
            var splits = date.split('-');
            var tmpriq = Number(splits[0] + "" + splits[1] + "" + splits[2]);
            var tmppotname = Number($("span.fa-check.caohaoSpan").attr('value'));

            if(activedate != tmpriq || activepotname != tmppotname)
            {
                alarm();
                activedate = tmpriq;
                activepotname = tmppotname;

                $.ajax({
                    type: "POST",
                    dataType: "json",
                    url: "${pageContext.request.contextPath}/wenduqx/conditionList",
                    data: {
                        gongqh: gongqhAndChejh % 100,
                        chejh: parseInt(gongqhAndChejh / 100),
                        riq: splits[0] + "" + splits[1] + "" + splits[2],
                        caoh: $("span.fa-check.caohaoSpan").attr('value'),
                        companyid: "${companyid}", // 注意：这里和下一行的字符串模板将不会在纯JavaScript中解析，需要替换为实际的值
                    },
                    success: function(response) {
                        histemperature = response;
                        resolve();
                    },
                    error: function(error) {
                        reject(error); // 拒绝 Promise
                    }
                });
            }
            else
            {
                cewendiandraw();
            }
        });
    }

    function drawCurve(){
        var selecteditems=updateSelectedItems();
        if(selecteditems.length > 0)
        {
            $.ajax(
                {
                    type: "POST",
                    dataType: "json",
                    url:"${pageContext.request.contextPath}/wenduqx/conditionList1",
                    data: {
                        caoh: $("span.fa-check.caohaoSpan").attr('value'),
                        companyindex: "${companyindex}"
                    },
                    success: function(response) {
                        var k=0;
                        selectednames=new Array();
                        selectedindex=new Array();
                        checkboxes.forEach(checkbox => {
                            if (checkbox.checked) {
                                var value=checkbox.value;
                                selectednames[k]=value.substring(0, value.indexOf(","));
                                selectedindex[k]=value.substring(value.indexOf(",") + 1);
                                k++;
                            }
                        });
                        var caohao=$("span.fa-check.caohaoSpan").attr('value');
                        var companyindex="${companyindex}";
                        var contextPath="${pageContext.request.contextPath}";

                        var temData = response;
                        recordindex_day = new Array();
                        for(i=1;i<=300;i++){
                            recordindex_day[i-1]=i;
                        }
                        // console.log(recordindex_day);
                        temps = new Array();
                        // console.log(temData);


                        if (temData!=null) {
                            for(var i=0;i<selectedindex.length;i++){
                                temps[i]=temData[selectedindex[i]];
                                console.log(temps[i]);
                            }
                            flag=1;
                            drawtem(selectednames,temps,recordindex_day,caohao,companyindex,selectedindex,contextPath);
                        }else{
                            alert("没有温度动态曲线！")
                        }
                    }
                });
        }else{
            alert("没有选择测温点！")
        }
    }

    function checkNum_2(val){
        var tmpvalue;
        if (val > 9) {
            tmpvalue = val.toString();
        }
        else {
            tmpvalue = "0" + val.toString();
        }
        return tmpvalue.toString();
    }

</script>

<footer class="fixed-bottom" style="max-height: 119px;">
    <div class="container-fluid" style="background-color: white;">
        <div class="row text-center " style="max-height: 47px;position: relative;top: -3.5px;">
            <div class="col-4">
                <p style="line-height: 15px;">
                    <a id="comebacklink" href="${pageContext.request.contextPath}/user/list?username=alwit" class="nav-link"
                       style="color: rgb(0,200,180);">
                        <i class="fa fa-home" aria-hidden="true"></i><br>
                        <span style="padding: 0px; font-size: smaller;">主页</span>
                    </a>
                    <script>
                        var tmpcomebacklink = "${pageContext.request.contextPath}/user/list?companyname=${companyname}&username=" + getCookieValue("username");
                        document.getElementById("comebacklink").href = tmpcomebacklink;
                    </script>
                </p>
            </div>
            <div class="col-4">
                <p style="line-height: 15px;">
                    <a href="${pageContext.request.contextPath}/user/mypage" class="nav-link" style="color: grey;">
                        <i class="fa fa-user-circle-o" aria-hidden="true"></i><br>
                        <span style="padding: 0px; font-size: smaller;">我的</span>
                    </a>
                </p>
            </div>
            <div class="col-4">
                <p style="line-height: 15px;">
                    <a href="${pageContext.request.contextPath}/user/morepage" class="nav-link" style="color: grey;">
                        <i class="fa fa-spinner" aria-hidden="true"></i><br>
                        <span style="padding: 0px; font-size: smaller;">更多</span>
                    </a>
                </p>
            </div>
        </div>
    </div>
</footer>
<script type="application/javascript" src="${pageContext.request.contextPath}/js/gongyiqx.js"></script>
<c:if test="${chejhandgongqh != null}">
    <script type="application/javascript">
        $(function () {
            $(".gongquAndChejianDiv").each(function () {
                var me = $(this);
                if (me.children("span:last-child").attr('value') == ${chejhandgongqh}) {
                    me.css('color', 'rgb(0,200,180)');
                    me.children("span:last-child").addClass("fa-check");
                }
            });
            var riq = String(${riq});
            var year = riq.substr(0, 4);
            var month = riq.substr(4, 2);
            var day = riq.substr(6, 2);
            riq = year + "-" + month + "-" + day;
            $("#date").val(riq);


            $(".caohaoDiv").each(function () {
                var me = $(this);
                if (me.children("span:last-child").attr('value') == '${caoh}') {
                    me.css('color', 'rgb(0,200,180)');
                    me.children("span:last-child").addClass("fa-check");
                }
            });
            <%--$(".cewendianDiv").each(function () {--%>
            <%--    var me = $(this);--%>
            <%--    if (me.children("span:last-child").attr('value') == '${gongyiqxlx}') {--%>
            <%--        me.css('color', 'rgb(0,200,180)');--%>
            <%--        me.children("span:last-child").addClass("fa-check");--%>
            <%--    }--%>
            <%--});--%>
        });
    </script>
</c:if>
</body>
</html>
