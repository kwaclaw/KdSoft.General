<?xml version='1.0' encoding='utf-8' ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html"/>
	<xsl:include href="Style1.xsl"/>

	<xsl:template match="/">
		<html>
			<body>
				<!-- Set Formatting Characteristics -->
				<xsl:call-template name="Style1"/>
				<h1>Product Information for different regions</h1>
				<xsl:apply-templates/>
			</body>
		</html>
	</xsl:template>

	<xsl:template match="inventory">
		<span class="subhead">Inventory Listing</span>
		<BR/>
		<xsl:apply-templates/>
	</xsl:template>

	<xsl:template match="region">
		<span class="subhead">Region</span>
		<span class="text">
			<xsl:value-of select="@area"/>
		</span>
		<br/>
		<br/>
		<table border="1">
			<tr>
				<th>ID</th>
				<th>Name</th>
				<th>Price</th>
				<th>Quantity</th>
			</tr>
			<xsl:apply-templates/>
		</table>Total Products in region:
		<xsl:value-of select="count(product)"/>
		<br/>Total Inventory in region:
		<xsl:value-of select="sum(product/quantity)"/>
		<br/>
		<br/>
	</xsl:template>

	<xsl:template match="product">
		<tr>
			<td>
				<xsl:value-of select="prodid"/>
			</td>
			<td>
				<xsl:value-of select="name"/>
			</td>
			<td>
				<xsl:value-of select="price"/>
			</td>
			<td>
				<xsl:value-of select="quantity"/>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
